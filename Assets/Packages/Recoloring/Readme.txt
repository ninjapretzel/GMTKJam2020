Here is a large collection of shaders to use for recoloring.
These are useful for providing users the ability to change colors of objects in-game.

They are slightly more costly than the equivelant shaders-

	The Clip variants do an extra texture lookup per pixel
		And the Clip HSV variants additionally do up to 4 HSV color conversions.
	The Palette variants do 4 or 8 comparisons and lerps.
	
	The Toon Lit-Unlit variant has custom lighting that is more expensive in most situations.
		The Unlit Toon variant is more expensive than the basic Unlit variant, since it's a surface shader.
		
	The sprite variants are also more expensive than the standard sprites.
	
It's not recommended to use these recoloring shaders for everything, but just for things that specifically need to be recolored,
or for things that have large textures, to cut down on total texture space when providing many variants.

Some good uses of these shaders:
	- Allowing players to customize colors on the player avatar
	- Allowing players to customize colors in the UI
	- Creating variants of enemies, NPCs or props without adding significant amounts of texture data
	- Recoloring environments to change their look and feel

To make full use of the shaders, a little bit of scripting might be needed.

	Properties to manipulate these materials via scripting:
		
		Clip Shaders: Probably more useful for assets without a distinct pallete
		
		NOTE: Not available for sprites, because it can be difficult to line up clip textures with sprite data
			Especially for animated sprites, the clip texture information would also need to be animated.
			
			_Clip - clip texture
			_Color - color of 'black' region
			_Color1 - color of 'red' region
			_Color2 - color of 'green' region
			_Color3 - color of 'blue' region
			
			Additionally, for HSV adjustment variants...
			_Saturation - saturation adjustment of 'black' region
			_HueShift - hue adjustment of 'black' region
			
			_Saturation1 - saturation adjustment of 'red' region
			_HueShift1 - hue adjustment of 'red' region
			
			_Saturation2 - saturation adjustment of 'green' region
			_HueShift2 - hue adjustment of 'green' region
			
			_Saturation3 - saturation adjustment of 'blue' region
			_HueShift3 - hue adjustment of 'blue' region
			
		Palette shaders: Probably more useful for assets with a distinct palletes
		
		Note: Best for Sprites, or other images with a distinct palletes, and little or no color blending
		
			_Tolerance - float determining how close colors need to be to be applied.
				Range: (0.01 to 0.65)
			_Strength - float determining how much to change the colors
				Range: (0.00 to 4.00)
			_ColorSearch1 - First color to search for
			_ColorTarget1 - Color to replace first color with
			_ColorSearch2 - Second color to search for
			_ColorTarget2 - Color to replace second color with
			...etc, 
			up to _ColorSearch4 and _ColorTarget4 or _ColorSearch8 and _ColorTarget8
			depending on the variant of the shader.
		
		
		Toon Shaders: The most complex shaders. There's more going on than just recoloring.
			The Clip or Palette Properties are also available depending on the shader variant. 
			
			These control the outline size.
				_OutlineVal - Float determining the base thickness of the outline
					Range: (0.001 to 4.0)
				_OutlineScreen - Float determining how much the screen's height affects the outline thickness.
					Range: (0.0 to 2.0)
				_OutlineDist - Float determining how much the distance to the camera affects the outline thickness.
					Range: (0.0 to 2.0)
				_OutlineCol - Color for outline
				
			These control the 'Rim lighting' effect.
				_RimColor - Color of rim light. Set to black for no lighting. Works best as a dark color.
				_RimPower - Float, decides how strong the rim is, lower values are brighter.
					Range: (0 to 8)
				
			These are get a little complex:
				_WrapMin: Float, minimum light wrapping around surface
					Range: (0 to 1)
				_WrapScale: Float, difficulty for light to wrap.
					Range: (0 to 2)
				_AttenBoostMin: Float, boost to light on surfaces.
					Range: (0 to 1)
				_AttenScale: Float, hard to describe the effect- kinda softens the falloff of attenuation.
					Range: (0 to 1)
					
				_RampBlend: Float, controls blending between normal lighting and ramped lighting.
					Range: (0 to 1)
				_LitUnlitBlend: Float, controls blending between final lighting and unlit color.
					Range: (0 to 1)
				_EnvironmentLight: Float, controls application of environment lighting. Applied ontop of unlit.
					Range: (0 to 1)
				
				
			