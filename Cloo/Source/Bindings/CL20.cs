#region License

/*

Copyright (c) 2009 - 2013 Fatjon Sakiqi

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

*/

#endregion

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Cloo.Bindings
{
	/// <summary>
	/// Contains bindings to the OpenCL 2.0 functions.
	/// </summary>
	/// <remarks> See the OpenCL specification for documentation regarding these functions. </remarks>
	[SuppressUnmanagedCodeSecurity]
	public class CL20 : CL12
	{
		/// <summary>
		/// See the OpenCL specification.
		/// </summary>
		[DllImport(libName, EntryPoint = "clCreatePipe")]
		public extern static CLMemoryHandle CreatePipe(
			CLContextHandle context,
			ComputeMemoryFlags flags,
			IntPtr pipe_packet_size,
			IntPtr pipe_max_packets,
			IntPtr properties,
			out ComputeErrorCode errcode_ret);

		/// <summary>
		/// See the OpenCL specification.
		/// </summary>
		[DllImport(libName, EntryPoint = "clGetPipeInfo")]
		public extern static ComputeErrorCode GetPipeInfo(
			CLMemoryHandle pipe,
			ComputePipeInfo param_name,
			IntPtr param_value_size,
			IntPtr param_value,
			out IntPtr param_value_size_ret);
	}
}
