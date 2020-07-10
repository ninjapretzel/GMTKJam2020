using BakaTest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ex.Utils {
	/// <summary> Utility to pool worker threads for repeated tasks on a collection. </summary>
	public class WorkPool<T> {

		/// <summary> Counter used to give IDs to new work pools  </summary>
		private static long nextID = 100000L;

		/// <summary> Class for running functions repeatedly, with ability to pause or stop work. </summary>
		public class Worker {
			/// <summary> Counter used to give IDs to new workers </summary>
			private static long nextID = 100000L;
			
			/// <summary> Number of times to iterate work per sleep (to prevent eating 100% cpu) </summary>
			private int workPerBreak = 100;
			/// <summary> MS to sleep </summary>
			private int sleepTime = 1;
			
			/// <summary> Iterations worked since last break </summary>
			private int workedThisBreak = 0;
			/// <summary> Total lifetime iterations worked </summary>
			public long worked { get; private set; }
			/// <summary> ID Assigned to this worker </summary>
			private long id = -1;
			/// <summary> Thread this worker is running in. </summary>
			private Thread thread = null;
			/// <summary> Is this worker paused? </summary>
			public bool isPaused { get; private set; }
			/// <summary> Is this worker alive? </summary>
			public bool isRunning { get; private set; }

			/// <summary> Exception handler </summary>
			public Action<Exception> errorHandler;

			public Worker(int? workPerBreak = null, int? sleepTime = null) {
				if (workPerBreak != null) { this.workPerBreak = workPerBreak.Value; }
				if (sleepTime != null) { this.sleepTime = sleepTime.Value; }
				id = Interlocked.Increment(ref nextID);
				//Console.WriteLine($"{this} created.");
			}

			/// <summary> Sets the exception handler. </summary> <param name="handler">Handler to run </param> <returns> Self </returns>
			public Worker OnError(Action<Exception> handler) { this.errorHandler = handler; return this; }

			public override string ToString() { return $"Worker<{typeof(T)}>.{id}"; }

			/// <summary> Unpauses the worker if possible. </summary>
			public void Unpause() {
				if (!isRunning || !isPaused || thread == null) {
					throw new InvalidOperationException($"Cannot Unpause {this} . State is: running?{isRunning} paused?{isPaused} thread?{thread}");
				}
				isPaused = false;
			}

			/// <summary> Pauses the worker if possible. </summary>
			public void Pause() {
				if (!isRunning || isPaused || thread == null) {
					throw new InvalidOperationException($"Cannot Pause {this} . State is: running?{isRunning} paused?{isPaused} thread?{thread}");
				}
				isPaused = true;
			}

			/// <summary> Starts the worker if possible. </summary>
			/// <param name="work"> Inner work function to run. </param>
			public void Start(Action work) {
				if (isRunning) { throw new InvalidOperationException($"{this} is already running! Stop worker before trying to restart it!"); }
				isRunning = true;
				isPaused = false;
				worked = 0;
				workedThisBreak = 0;

				// Wrap work function in another thread to loop 
				thread = new Thread(() => {
					Thread self = thread;
					while (isRunning && (thread == self)) {
						//Thread.MemoryBarrier();
						try {
							if (isPaused) {
								ThreadUtil.Hold(sleepTime);
							} else {
								work();
								worked++;
								workedThisBreak++;
							}

							if (workPerBreak > 0 && workedThisBreak > workPerBreak) {
								workedThisBreak = 0;
								// Waiting...
								//Console.WriteLine($"{this} is waiting... running?{isRunning} paused?{isPaused}");
								ThreadUtil.Hold(sleepTime);
							}
						} catch (Exception e) {
							errorHandler?.Invoke(e);
						}
					}
				});
				thread.Name = $"{this}";
				thread.Start();
			}

			public void Stop() {
				if (!isRunning) { throw new InvalidOperationException($"{this} Cannot stop when not started!"); }
				Console.WriteLine($"{this} : stopped.");
				isRunning = false;
				thread = null;
			}

		}
		
		/// <summary> Delegate for function provided to do the work. </summary>
		/// <param name="item"> Thing to work on, passed into the function.</param>
		public delegate void WorkFn(T item);
		
		/// <summary> Items currently being processed for concurrent access by workers. </summary>
		private ConcurrentQueue<T> workItemQueue;

		/// <summary> Temporary set of removed items.
		/// Used to prevent items from being added back to queue if they are being worked on. </summary>
		private ConcurrentSet<T> removedTemp;
		/// <summary> All items currently being worked on. </summary>
		private ConcurrentSet<T> workItems;
		/// <summary> Workers that are currently busy. </summary>
		private ConcurrentSet<Worker> liveWorkers;
		/// <summary> Pre-created workers that can be re-used. </summary>
		private ConcurrentSet<Worker> sleptWorkers;
		/// <summary> Inner work function. </summary>
		private WorkFn workFn;
		/// <summary> Exception Handler </summary>
		private Action<Exception> errorHandler;

		/// <summary> Target number of work units per worker. </summary>
		private int workSize = 100;
		/// <summary> Target iterations of work per sleep. Used to prevent eating 100% CPU for no reason. 
		/// Set &lt; 1 to allow 100% CPU usage. </summary>
		private int workPerBreak = 100;
		/// <summary> Time to sleep if there is no work or a break has been reached. </summary>
		private int sleepTime = 1;
		/// <summary> ID assigned to this work pool. </summary>
		private long id = -1;

		/// <summary> Number of work items in work set </summary>
		public int WorkItemCount { get { return workItems.Count; } }
		/// <summary> Number of live workers. </summary>
		public int LiveWorkerCount { get { return liveWorkers.Count; } }
		/// <summary> Number of slept workers. </summary>
		public int SleptWorkerCount { get { return sleptWorkers.Count; } }

		/// <summary> Is this worker done? </summary>
		public bool stopping { get; private set; }

		public WorkPool(WorkFn workFn, int? workSize = null, int? workPerBreak = null, int? sleepTime = null) {
			this.workFn = workFn;
			if (workSize != null) { this.workSize = workSize.Value; }
			if (workPerBreak != null) { this.workPerBreak = workPerBreak.Value; }
			if (sleepTime != null) { this.sleepTime = sleepTime.Value; }
			Init();
		}

		private void Init() {
			id = Interlocked.Increment(ref nextID);
			workItemQueue = new ConcurrentQueue<T>();
			removedTemp = new ConcurrentSet<T>();
			workItems = new ConcurrentSet<T>();
			liveWorkers = new ConcurrentSet<Worker>();
			sleptWorkers = new ConcurrentSet<Worker>();
			// Add first worker
			liveWorkers.Add(MakePoolWorker());
		}

		public WorkPool<T> OnError(Action<Exception> handler) { errorHandler = handler; return this; }

		public override string ToString() {
			return $"WorkPool<{typeof(T)}>.{id} ({workItems.Count} items / {liveWorkers.Count} live / {sleptWorkers} sleeping)";
		}

		/// <summary> Adds the given workItem into the pool. </summary>
		/// <param name="workItem"> Item to add to the pool </param>
		public void Add(T workItem) {
			workItemQueue.Enqueue(workItem);
			workItems.Add(workItem);
			BalanceWorkers();
		}

		/// <summary> Removes the WorkItem instance from the pool.
		/// If it is currently being worked on, it will be removed when work is finished.
		/// Otherwise, when it is next encountered, it is discarded. </summary>
		/// <param name="workItem"> Item to remove from pool. </param>
		public void Remove(T workItem) {
			if (workItems.Contains(workItem)) {
				// Remove from work set
				workItems.Remove(workItem);
				// Remember to discard the item later
				removedTemp.Add(workItem);
			}
			BalanceWorkers();
		}

		/// <summary> Stops all future work. </summary>
		public void Finish() {
			Console.WriteLine("Finishing...");
			foreach (Worker w in liveWorkers) { w.Stop(); }
			Console.WriteLine($"Stopped {liveWorkers.Count} live workers...");
			foreach (Worker w in sleptWorkers) { w.Stop(); }
			Console.WriteLine($"Stopped {sleptWorkers.Count} slept workers...");

			liveWorkers.Clear();
			sleptWorkers.Clear();
			removedTemp.Clear();
			workItems.Clear();
			Console.WriteLine("Cleared all data sets");
			int i = 0;
			T item;
			while (workItemQueue.TryDequeue(out item)) { i++; }
			Console.WriteLine($"Cleared {i} work items");
		}

		/// <summary> Sees if a worker needs to be added or removed, and does so </summary>
		private void BalanceWorkers() {
			int target = 1 + workItems.Count / workSize;
			int size = liveWorkers.Count;
			//Log.Debug($"Pool {id} at { size} balancing to {target} at { workItems.Count } / {workSize} ");

			if (size < target) {
				Worker worker;
				if (sleptWorkers.Any()) {
					worker = sleptWorkers.First();
					sleptWorkers.Remove(worker);
					worker.Unpause();
					//Log.Debug($"Woke worker {worker}");
				} else {
					worker = MakePoolWorker();
					//Log.Debug($"New worker {worker}");
				}
				liveWorkers.Add(worker);

			} else if (size > target) {

				/// Sleep last live worker.
				Worker worker = liveWorkers.Last();
				//Log.Debug($"Slept worker {worker}");
				liveWorkers.Remove(worker);
				worker.Pause();
				sleptWorkers.Add(worker);

			}

		}

		/// <summary> Creates a new worker for the pool </summary>
		/// <returns> A new worker configured for this pool. </returns>
		private Worker MakePoolWorker() {
			Worker worker = new Worker(workPerBreak, sleepTime).OnError(errorHandler);

			worker.Start(() => {
				T item;
				if (workItemQueue.TryDequeue(out item)) {
					if (removedTemp.Contains(item)) {
						return; 
					}
					try {
						workFn(item);
						//Console.WriteLine($"Worked on {item} in {worker} / {this} ");
					} catch (Exception e) {
						throw new Exception($"Error in WorkPool {id}: ", e);
					} finally {
						if (removedTemp.Contains(item)) {
							removedTemp.Remove(item);
						} else {
							workItemQueue.Enqueue(item);
						}
					}
				} else {
					//Console.WriteLine($"No work to do, skipped in {this}");
					try { ThreadUtil.Hold(sleepTime); } 
					catch {}
					//catch (Exception e) {
					//	throw new Exception($"WorkPool {id} thread interrupted!", e);
					//}
				}
			});

			return worker;
		}
	}
	
	/// <summary> Tests for workpool. </summary>
	public static class WorkPool_Tests {
		public class TestMap {
			public string name { get; private set; }
			public TestMap(string name) { this.name = name; }
			public void Update() { }
			public override string ToString() { return name; }
		}
		public static void Wait(int ms) { try { Thread.Sleep(ms); } catch (Exception) { } }

		public static void TestInternal() {
			int workSize = 100;
			int workPerBreak = 1000;
			int sleepTime = 3;
			int failures = 0;
			WorkPool<TestMap> workPool = new WorkPool<TestMap>((map)=> { map.Update(); }, 
				workSize, workPerBreak, sleepTime)
				.OnError((e)=>{ Interlocked.Increment(ref failures); });

			try {
				
				workPool.WorkItemCount.ShouldBe(0);
				workPool.LiveWorkerCount.ShouldBe(1);
				workPool.SleptWorkerCount.ShouldBe(0);

				int numMaps = 507;
				List<TestMap> maps = new List<TestMap>();
				for (int i = 0; i < numMaps; i++) { maps.Add(new TestMap($"Level {i}")); }

				Wait(20);
				failures.ShouldBe(0);
				workPool.WorkItemCount.ShouldBe(0);
				workPool.LiveWorkerCount.ShouldBe(1);
				workPool.SleptWorkerCount.ShouldBe(0);

				for (int i = 0; i < maps.Count; i++) {
					workPool.Add(maps[i]);
					if ((i+1) % 50 == 0) {
						Wait(1);
					}
				}

				failures.ShouldBe(0);
				workPool.WorkItemCount.ShouldBe(507);
				workPool.LiveWorkerCount.ShouldBe(6);
				workPool.SleptWorkerCount.ShouldBe(0);

				Wait(20);
				failures.ShouldBe(0);
				workPool.WorkItemCount.ShouldBe(507);
				workPool.LiveWorkerCount.ShouldBe(6);
				workPool.SleptWorkerCount.ShouldBe(0);
			
				for (int i = 0; i < maps.Count; i++) {
					workPool.Remove(maps[i]);
					if ((i+1) % 50 == 0) {
						Wait(1);
					}
				}

				Wait(20);
				failures.ShouldBe(0);
				workPool.LiveWorkerCount.ShouldBe(1);
				workPool.SleptWorkerCount.ShouldBe(5);
				
			} catch (Exception e) {
				workPool.Finish();
				throw e;
			}

			workPool.Finish();
		}


	}
}
