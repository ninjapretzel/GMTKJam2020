# Procedural Surface Shaders

#### Thank you for purchasing this package.
Thanks for your interest in these procedural surface textures.
These shaders are good for texturing things quickly, creating a variety of surfaces in your game, or creating animated surfaces.

---
##### Contains eight procedural effects:
* __Lumpy__, A voroni-cell based noise. 
* __LumpyWet__, A modifcation to the above effect that has animated "liquid" flowing.
* __Bricks__, An effect that specialises in creating brick-like surfaces.
* __BricksWet__, A modifcation to the above effect that has animated "liquid" flowing.
* __Marble__, An effect primarily containing lines distorted by noise.
* __Moon__, An effect which specializes in cratered landscapes
* __Camo__, An effect which specializes in camo splat like patterns.
* __VTech__, A technological looking effect. Heavily modified from the "Digital Brain" effect by struss.
---
##### Demos
Some scenes are included for the purpose of demonstrating how to use this shader.
- In the included "demo" scene, A number of preview variants of the effects are present.
- In the included "aliasing demo" scene, rounding error artifacts are shown.
    - Double click one of the "Focus Me" objects in the hierarchy to be moved nearby to a given set of objects.
---
##### General Tips and Notes
- These shaders can potentially require a large amount of graphics horsepower to render
    - For best practice, try to put an option for players on PC to tone down procedural effects
        - These options should pimarily affect properties such as "Fancy Parallax" and "Depth Layers"
    - If options are not an option (*ha*), you probably still want to know what is an issue:
        - Play the game on older hardware or have someone else do so, and report to you what causes slowdown
        - Detect the platform and change shaders internally
        - Raise your minimum requirements
    - Some perfomance slider options such as _"Depth Layers"_ and _"Crater Octaves"_ have ludicrous maxes
        - This is so that when graphics hardware becomes capable, they can be taken advantage of
        - However, It's still possible to pump out-of-range values into these materials from a script
- Effects may look different across different graphics hardware
    - Floating point rounding errors can greatly differ based on what the graphics hardware can work with
        - Most of the "Modern" cards support 32-bit floating point formats by default
        - On older/integrated/mobile hardware, 8-bit, 16-bit, and 24-bit floating point formats may be used
        - Even if higher formats are specified, they may not be available on the platform.
    - Errors can cause textures to "break down" and look blocky, especially in large worlds.
        - Moving the "_Offset" property of most effects can be used to reduce this, by moving the texture near the origin.
        - It might not be possible to entirely prevent this on certain hardware.
- These shaders _DO NOT_ batch. This is by design. 
    - Batching throws multiple meshes into one, which throws off how the verts of the mesh get processed.
    - This means that innately, there's no benefit to different meshes having different materials
    - Another good idea is to use the largest meshes possible for optimized draw calls
        - Want all of your floors in a level to share the same brick texture? Separate them all into one mesh.
        - All of the walls need to have the same brick texture? Put all of those into a separate mesh as well.
- Don't design effects by staring super close to them when designing them. 
    - It may be very nice to be able to stay detailed very close to surfaces made with these shaders, But:
    - Best practice is to design the surface from the distance it's expected to typically be viewed from
    - Effects may look very blocky from up close, depending on Octaves/Persistance values.
    - Effects may also look very pixelated from far away, even with MSAA on! (Procedural textures don't have mipmaps!)

---
---
---
## __Shader Properties__
Here are some descriptions of the properties found inside of these shaders.

---
## Common Properties
There are a number of common properties for the included shaders:
#### __Variant Toggles__
##### These toggles change core features of the shader, and can drastically change the way the surface looks, and how the surface performs (both rendering speed and how it changes if the object moves)
- __"Use Worley Noise"__: _Toggles the shader between using "Worley" and "Manhattan" distances_
    - Enabled: _Generator uses "Worley" distance, creates cells with lines at any angle_
    - Disabled: _Generator uses "Manhattan" distance, creates cells with lines at 90/45 degree angles_
- __"Use Worldspace Position"__: _Toggles the shader from using the position of the pixel in world/local space._
    - Enabled: _Makes the object textured based on where it exists in worldspace_
        - Makes it easy for multiple objects to share the same material, with the seam between the objects typically being invisible, so long as they line up.
	    - Also makes it so that if the object moves/rotates, the texture does too, so make sure that objects using "worldspace" are static (don't move/rotate)
    - Disabled: _Makes the object textured based on its meshspace coordinates_ 
        - Multiple objects with the same mesh/material will look identical._
	    - Also makes it so that the object can move without the texture moving
		- With the exception of skinned/animated meshes, where meshspace coordinates move
	- Remarks: 
	    - It's not likely that material settings can be easily shared between variants with this option enabled/disabled. Various texture scales may need to be changed.
- __"Fancy Parallax"__: _Toggles the parallax mode used by the shader._
	- Enabled: _Shader uses a short raycast in a depthfield for parallax. Can be slow, depending on how deep the raycast needs to go on average._
		- Also makes the surface more consistant, and only in a smaller depth range, due to how the raycast works
	- Disabled: _Shader uses a single sample of a depthfield for parallax. Much faster, especially on older cards._
		- Also makes the surface less consistant, and takes values in a wider depth range. Can be used to create some interesting highly warped looking glassy surfaces.
	- Remarks:
		- Having this enabled typicaclly leads to a much more pleasing look for the surface
		- It's recommended to have a quality option to enable/disable this feature, to make sure that the game can still run reasonably well on older cards that _can_ support it, but are slow.
		- Also, if "Parallax amount" is set to 0, you probably want to disable this option.
---
#### __Colors__
##### Controls for the colors used for the surface.
- Most of the time these are pretty self explanitory.
- Changing these will change the general color/brightness of the surface
---
#### __Surface Property__
##### Controls for the PBR details of the surface. 
- __"Polish"__ _Amount of "Glossiness" and "Metallic" applyed based on the "Height" of the sample._
    - Setting this very high can easily make the surface overblow the intended range for the other parameters.
- __"Glossiness"__ _Unity 5 Standard PBR property. Controls how much light the surface reflects._
- __"Metallic"__ _Unity 5 Standard PBR property. Controls how much the surface color is obscured by reflections._
---
#### __Noise Settings:__
##### General Controls for texture generation
- __"Seed"__: _Value used in the pseudo random number generator._
	- Changing this superficially changes the look of the surface.
	- Used by pretty much all layers of textures.
	- Just needs to be a decently high value. Suggested range (1,000 - 100,000)
	- Sometimes lower values (< 1,000) can work, but it can depend on other settings.
- __"Octaves"__:  _Controls the number of "octaves" of noise applied for textures._
	- Overrided by some textures (like "bump")
	- Higher values make a more complex texture, and only slightly worse performance.
	- Lower values make a simpler texture, and only slightly better performance.
- __"Persistence"__: _Controls how much power the 'deeper octaves' of noise have over textures._
	- Overrided by some textures (like "bump")
	- Higher values make a rougher texture.
	- Lower values make a simpler texture.
- __"Scale"__: _Controls the base scale of generated textures_
	- Overrided by some textures (like "bump")
	- Higher values make a more busy texture
	- Lower values make a more sublte texture
---
#### __Bump Texture__: 
##### Controls for the Generated "Bump" texture.
- __"Bump Octaves"__: _Controls the number of 'octaves' of noise that are compiled for the 'bump' texture._
	- Higher values make a more complex texture, and only slightly worse performance.
	- Lower values make a simpler texture, and only slightly better performance.
- __"Bump Persistence"__: _Controls how much power the 'deeper octaves' of noise have on the 'bump' texture._
	- Higher values make a rougher texture.
	- Lower values make a simpler texture.
- __"Bump Spread"__: _Controls the base scale of the bump texture._ 
	- Larger values make more smaller 'bumps' in the same area.
	- Smaller values make fewer large 'bumps' in the same area.
- __"Bump Amount"__: _Controls the intensity of the bump texture._
	- Larger values make a less smooth- looking surface.
	- Smaller values make a smoother looking surface.
---
#### __Parallax Settings__: 
##### Controls for the parallax effect.
###### _This effect makes the surface seem to have depth, even if it's applied to flat polygons._
- __"Parallax Amount"__: _Controls the maximum depth of the surface._
	- May need to be anywhere between 0 and 1000, depending on the scale of the surface.
	- Larger values make a bigger parallax effect (more percieved depth)
	- Smaller values make the surface seem more flat.
- __"Depth Layers"__: _Controls the number of layers used during the "Fancy Parallax" raycast._
	- More layers mean the surface will be more detailed, but also can SEVERLY affect performance.
	- It may be a good idea to include a user-controllable setting for this parameter, to allow users to reduce the number of layers to improve performance.
- __"Height Texture Scale"__: _Controls the scale of the FBM texture applied to distrupt height_
	- Larger values mean more height fluctuations in the same area
	- Smaller values mean less height fluctuations in the same area
- __"Height Texture Power"__: _Controls the amount of height applied by a FBM texture to distrupt the height across a surface._
    - Larger values make the height distrupted more
	- Smaller values make the height distrupted less
- __"Water Height"__: _Amount of height that the 'liquid' can occupy in wet shaders._
	- Values from -1 to 1 tend to work the best, especially with "Fancy Parallax" disabled.
---			
#### __Texture Offsets__
##### Offset applied to meshspace/worldspace positions used to generate the texture
- __"Noise Offset"__
	- General offset used to offset the texture as a whole.
	- Final offset is (x, y, z) * w, as stated on the label.
	- This can be used to pan the texture across a surface for a desired look.
	- It can also be used to line up the textures on objects that do not have "Use Worldspace Position" enabled, but can't share meshes, or might need to move.
	- Could also be used to move textures to 'resonable' positions on worldspace textured objects at insane positions (0, 100000, 0)
---
#### __Common Voroni Controls__
##### Used to control the celluar noise that comprises various effects.
- __"Voroni Composition/Crater Composition"__: Controls the general look&feel of the cell noise that is generated.
	- The voroni-cell noise is based off of the distance from any point to a set of nearby points.
	- This vector controls how the 3 closest point values affect the "value" of that pixel.
		- __X__: multiplier of distance to 1st closest point
		- __Y__: multiplier of distance to 2nd closest point
		- __Z__: multiplier of distance to 3rd closest point
	    - __W__: multiplier ontop of (x,y,z)
	- Some ways to set this up:
		- __Rolling Lumps__: Set X, Y, Z to add up to 0 (or close to) (ex, 1, -.5, -.5) and use W to change the 'height' as desired
		- __Stonelike__: Set X, Y, Z to add up to 2 (or close to) (ex, 1.02, .25, .67) and use W around .5
		- Experiment with the settings, this is a good setting to play with!
- __"Voroni Scale"__: Controls the voroni-cell texture scale.
	- Larger values make more cells in the same area
	- Smaller values make fewer cells in the same area
- __"Voroni Layer Blending"__: Used to control the effect that layers of voroni noise have on water.
	- x/y/z/w values control layers of high to low frequency (respectively)
---	
#### __Other Common Controls__
- __'X Panning'/'X Pan'__: Used to control how an animated texture pans across a surface.
	- Controls both the direction and speed of the panning.
	- The used vector is the (x, y, z) vector multiplied by the w component.
- __'X Smoothing'__: _Changes how the noise is smoothed or made rougher._
	- Values near from 0 to .5 have sharper features
	- Values near from .5 to 1 have smoother features
---
---
---
## Individual Properties
##### Some shaders have their own properties:
---
#### __Lumpy / LumpyWet__
- Uses only common properties.
---
#### __Camo__
- __"Clips"__: _Controls how the different layers of the camo splat texture are placed._
	- y/z/w control cutoffs for the second/third/fourth colors.
	    - Values near 1 prevent more of that color from appearing
		- Values near 0 allow more of that color to appear.
- __"Stretch"__: _Controls how much the texture is stretched in different directions._
	- Fairly useful to change the general look of the texture
	- Setting it too high in one or two directions (x,y,z) doesn't look very nice
- __"Difference Noise Jump"__: _Changes the spread of the splats by adjusting the jump between samples._
	- Kind of like seed, this can really be set to any value, and is another way to make superficial variations of the texture
- __"Difference Noise Layers"__: _Changes the number of difference noise layers that are applied._ 
	- Has a very slight performance cost, based on the number of Octaves used.
	- Setting this higher tends to make more smaller splotches surrounding the larger ones.
---
#### __Marble__
- __"Occlusion Scale"__: _Controls how much light gets trapped inside the 'deeper' areas of the material._
    - Positive values make light get trapped, darkening the deeper areas of the material
	- Negative values make more light come out of the material than goes in.
- __"Marbling"__: _Changes the axis and scale of the changes in color across the surface._
- __"Depth Bias"__: _Moves the base position for the depth of the surface._
	- Lower values make the entire surface deeper.
	- Higher values make the entire surface more shallow.
---
#### __Moon__
- __"Crater Octaves"__: _Controls the number of layers of craters created._
	- High values are a pretty major performance hit on low-end cards. Especially combined with fancy parallax, and lots of depth layers.
- __"Crater Octave Scale"__: _Controls the boost in frequency between layers of craters._
	- Pretty good values are between 1.5 and 3
- __"Crater Shift"__: _Controls the offset direction/distance for craters_
	- Useful to manipulate to reduce/tweak visible repetition lines of craters.
	- (0,0,0,0) leaves craters aligned to a grid
	- (1,1,1,1) works well for most cases.
- __"Crater Max"/"Crater Min"__: _Controls the extents of what is and is not in a crater._
	- Everything at or below the minimum is the bottom of the crater.
	- Everything above max is outside of the lip of the crater.
	- Anything inside is the edge of the crater.
- __"Crater/Surface Texture Nudging"__: _Changes the blending between the crater/surface textures._
	- Values below 0 bleed the crater texture onto the surface.
	- Values above 0 bleed the surface texture into craters.
- __"Detail Scale"__: _Changes the scale of the 'detail' texture laid ontop of everything._
---
#### __VTech__
- __"Albedo Brightness"/"Emissive Brightness"__: _Controls the scale of the Albedo/Emissive colors outside of the (0, 1) range (HDR essentially)_
- __"Frequency Per Octave"__: _Controls the boost in frequency between layers of noise._
	- Pretty good values are between 1.5 and 3
- __"Electron/Trace Seed"__: _Secondary seed used to add differences between samples when calculating 'trace' and 'electron' lines._
	- For a """PURE""" texture, set this to the same value as "Seed"
- __"Trace Composition"__: _Controls how "Trace" lines are calculated._
	- Fairly complex calculation, hard to describe individual components, here's my best try:
	- X and Y together control how two noise samples are compared. 
		- They work best when somewhat similar.
		- Sometmes small, negative values for X work well.
		- X typically should be less than Y, "Best" values for X range from (-Y, Y)
		- Reversing their relationship inverts the "Traces".
		- Setting them equal makes the traces aliased. Use this information how you will.
	- Z controls how the trace lines 'repeat' parallel to themselves.
	- W is just a bit werid. It controls an offset into a noise function.
		- Setting W to 0 or .1 works pretty well for most uses
		- Setting it very high (30,000+) can alias the traces while leaving the electrons smooth. 
			- (May be useful for style, but unpredictable effects on different graphics cards)
- __"Trace Strength"/"Electron Strength"__: _Control the brightness of the "Trace" lines and the "Electrons" respectively._
	- Higher values make that component brighter
	- Lower values make that component darker.
	- One set positive and the other negatve makes the negative one "remove" the positive one where they intersect.
- __"Electron Start Layer"__: _Controls the octave at which electrons start to be rendered._
	- Note: Electrons often are brighter than just the panning effect.
- __"Electron Composition"__: _Controls how the "Electron" lines are calculated._
	- Like "Trace Composition", this is a fairly complex calculation, so here is my best explanation of how these work:
		- X controls thickness of the "Electron" lines.
			- Setting this bigger than max of the "Trace Composition" x/y values creates "Electron"s that bleed outside of the traces.
		- Y controls the length of the moving part "Electron" lines
			- This effect works best when X and Y are close in value
		- Z controls the base brightness of the entire "Electron". Has a bigger effect on low-octave electrons.
		- W controls the brightness of only the moving part of the "Electron".
		- Combined with "Electron Strength", this can be used to create a large variety of "Electron" patterns.
- __"Apply Animated Texture Movement"__: _Toggles the application of a built-in oscillating texture movement._
	- Controlled by the two properties near the bottom, "Noise Movement Amount" and "Noise Movement Speed"
- __"Noise Movement Amount"__: _Controls the distance that the texture moves when movement is animated._
	- Maximum movement is (x, y, z) * w
	- Setting (x,y,z) to 0 will nullify the movement across that axis.
	- Setting w to 0 will nullify movement across any axes.
- __"Noise Movement Speed"__: _Controls the speed at which the texture movement animation happens_
	- Position of movement is sin(time * (x,y,z) * w)
	- Setting (x,y,z) to zero will nullify the movement across that axis.
	- Setting w to 0 will nullify movement across any axes.
---			
#### __Bricks / BricksWet__
- __"Brick Size"__: _Controls the size of tiles across given axes._
    - Typically should be about the same size on two axes, and shorter or larger on one.
    - w does nothing.
- __"Brick Offsets"__: _Controls the offset of bricks per step in another direction_
    - A value of (0,0,0) will leave bricks aligned to a grid
    - w does nothing.
- __"Overall Scale"__: _Controls the scale of the entire texture_
    - Changes repeat scale for the brick pattern, as well as the texture on the bricks.
- __"Inner Border"/"Outer Border"__: _Control extents of what is considered a brick or mortar_
    - "Inner Border" being greater than "Outer Border" will invert the bricks (bricks and mortar are swapped)
- __"Mortar Texture Scale"__: _Controls the scale of the mortar texture_
- __"Brick Texture Scale"__: _Controls the scale of the brick texture_
- __"Marbling Scales"__: _Controls the rate of marbling across the brick's surface_
    - The selected brick color is blended with "Brick Marbling Color"
    - Marbling is calculated separately across (x,y,z) axes, with w used as a multiplier
    - This is a fairly sensitive control. W is a bit less sensitive than (x,y,z)
    - Values between (0,0,0,0) and (1,1,1,5) work best. 
- __"Marbling Params"__: _Controls some multipliers used during the marbled texture calculation_
    - These are just names that I think fit how the surface changes.
	- X - "Roughness"
	- Y - "Refraction"
	- Z - "Graininess"
    - w - "Offset"
- __"Color Spread"__: _Controls the distribution of different colored bricks_
    - Used to select the color of the brick based on position. 
    - Most settings with (x,y,z) all above 1 end up creating some pseudo random distributions
    - Can be used to make the bricks change color only across one direction, 
        - ex (0, 1, 0, 1) will make layers of brick across the y direction have the same colors
- __"Color Blending"__: _Controls how the colors of bricks are blended together._
    - x/y controls how much the color spread selected color is blended.
        - works best when both are inside (-1,1)
    - z controls how much the marbling is blended into the texture
    - w controls how much of the second color is subtracted from the marbling
---
