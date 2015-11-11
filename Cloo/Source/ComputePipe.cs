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
using System.Diagnostics;
using System.Threading;
using Cloo.Bindings;

namespace Cloo
{
	/// <summary>
	/// Represents an OpenCL pipe.
	/// </summary>
	/// <remarks> A memory object that connects two endpoints to transmit and receive an ordered sequence of data items. </remarks>
	/// <seealso cref="ComputeMemory"/>
	public abstract class ComputePipe : ComputeMemory
    {
        #region Properties

		/// <summary>
		/// Gets or sets (protected) the size in bytes of each packet delivered through the <see cref="ComputePipe"/>.
		/// </summary>
		/// <value> The size in bytes of each packet delivered through the <see cref="ComputePipe"/>. </value>
		public int PacketSize { get; protected set; }

		/// <summary>
		/// Gets or sets (protected) the capacity of the <see cref="ComputePipe"/>.
		/// </summary>
		/// <value> The maximum number of packets enqueued in the <see cref="ComputePipe"/>. </value>
		public int MaxPackets { get; protected set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new <see cref="ComputePipe"/>.
		/// </summary>
		/// <param name="context"> A valid <see cref="ComputeContext"/> in which the <see cref="ComputePipe"/> is created. </param>
		/// <param name="packetSize"> The size in bytes of each packet delivered through the <see cref="ComputePipe"/>. </param>
		/// <param name="maxPackets"> The maximum number of packets enqueued in the <see cref="ComputePipe"/>. </param>
		public ComputePipe(ComputeContext context, int packetSize, int maxPackets)
			: base(context, ComputeMemoryFlags.None)     //Allocation and usage flags are not used for pipes
		{
			ComputeErrorCode error = ComputeErrorCode.Success;
			Handle = CL20.CreatePipe(context.Handle, ComputeMemoryFlags.None, new IntPtr(packetSize), new IntPtr(maxPackets), IntPtr.Zero, out error);
			ComputeException.ThrowOnError(error);

			Init();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="context"></param>
		private ComputePipe(CLMemoryHandle handle, ComputeContext context)
			: base(context, ComputeMemoryFlags.None)
		{
			Handle = handle;

			Init();
		}

        #endregion

        #region Protected methods

        /// <summary>
        /// 
        /// </summary>
        protected void Init()
        {
            SetID(Handle.Value);

			PacketSize = (int)GetInfo<CLMemoryHandle, ComputePipeInfo, IntPtr>(Handle, ComputePipeInfo.PacketSize, CL20.GetPipeInfo);
			MaxPackets = (int)GetInfo<CLMemoryHandle, ComputePipeInfo, IntPtr>(Handle, ComputePipeInfo.MaxPackets, CL20.GetPipeInfo);

            Trace.WriteLine("Create " + this + " in Thread(" + Thread.CurrentThread.ManagedThreadId + ").", "Information");
        }

        #endregion
    }
}