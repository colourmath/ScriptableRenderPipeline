/**
* MIT License
*
* Copyright (c) 2018 Joseph Pasek
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
**/

#ifndef COLOURMATH_SHADERVARIABLES_INCLUDED
#define COLOURMATH_SHADERVARIABLES_INCLUDED

// Global lighting data
CBUFFER_START(GlobalLightData)
	// view-space lighting 
    fixed4 globalLightColors[8];
    float4 globalLightPositions[8];
    float4 globalLightAtten[8];
    int4  globalLightCount;
CBUFFER_END

// Convenience macros
#define LIGHT_POS(id) globalLightPositions[id]
#define LIGHT_COLOR(id) globalLightColors[id]
#define LIGHT_ATTEN(id) globalLightAtten[id]

#endif // COLOURMATH_SHADERVARIABLES_INCLUDED
