using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.IO;
using System;

namespace ProjectionScreenWindows
{
    public static class CreateGamePNGs
    {
        //transparent border is added so projectionshader works correctly
        //crop, left-bottom-right-top, right and top need to be negative to crop
        public static Texture2D AddTransparentBorder(ref Texture2D texIn, int[] crop = null)
        {
            int width(int[] a) { return a[2] - a[0]; }
            int height(int[] a) { return a[3] - a[1]; }

            if (texIn == null || texIn.width <= 0 || texIn.height <= 0)
                return texIn;

            //get new sides of image with crop
            int[] offsets = { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top
            if (crop != null && crop.Length >= 4)
                for (int i = 0; i < crop.Length; i++)
                    offsets[i] += crop[i];

            //check valid crop, if not valid --> ignore crop
            if (width(offsets) <= 0 || height(offsets) <= 0)
                offsets = new int[] { 0, 0, texIn.width, texIn.height }; //left-bottom-right-top

            Texture2D texOut = new Texture2D(
                width(offsets) + (2*FivePebblesPong.CreateGamePNGs.EDGE_DIST), 
                height(offsets) + (2*FivePebblesPong.CreateGamePNGs.EDGE_DIST), 
                TextureFormat.ARGB32, 
                false
            );

            //transparent background
            FivePebblesPong.CreateGamePNGs.FillTransparent(ref texOut);
            texOut.Apply();

            //copies via GPU
            UnityEngine.Graphics.CopyTexture(
                texIn, 0, 0, offsets[0], offsets[1], 
                width(offsets), height(offsets), texOut, 0, 0, 
                FivePebblesPong.CreateGamePNGs.EDGE_DIST, 
                FivePebblesPong.CreateGamePNGs.EDGE_DIST
            );

            return texOut;
        }
    }
}
