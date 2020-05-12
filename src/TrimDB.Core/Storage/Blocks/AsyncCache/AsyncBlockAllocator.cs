﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TrimDB.Core.Storage.Blocks.AsyncCache
{
    public class AsyncBlockAllocator : MemoryPool<byte>
    {
        private byte[] _slab;
        private ConcurrentQueue<int> _availableOffsets = new ConcurrentQueue<int>();
        private ConcurrentQueue<AsyncBlockManager> _ableToFree = new ConcurrentQueue<AsyncBlockManager>();
        private int _blockSize;
        private GCHandle _handle;

        public AsyncBlockAllocator(int numberOfBlocks, int blockSize)
        {
            _blockSize = blockSize;
            _slab = new byte[blockSize * numberOfBlocks];
            _handle = GCHandle.Alloc(_slab, GCHandleType.Pinned);

            for (var i = 0; i < numberOfBlocks; i++)
            {
                _availableOffsets.Enqueue(i * blockSize);
            }
        }

        public override int MaxBufferSize => _blockSize;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
        {
            int offset;
            while (true)
            {
                if (!_availableOffsets.TryDequeue(out offset))
                {

                    while (_ableToFree.TryDequeue(out var freeCandidate))
                    {
                        if (freeCandidate.TryToFree())
                        {
                            break;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            //TODO ADD SOME KIND OF SPIN WAIT, WAIT AND ALSO EXPANSION OF MEMORY

            return new AsyncBlockManagedMemory(this, offset, _blockSize);
        }

        internal void AddFreeCandidate(AsyncBlockManager blockManager) => _ableToFree.Enqueue(blockManager);

        protected override void Dispose(bool disposing) => _handle.Free();

        private void Return(int offset) => _availableOffsets.Enqueue(offset);

        private unsafe void* GetPointer(int offset, int elementId) => (void*)IntPtr.Add(_handle.AddrOfPinnedObject(), offset + elementId);

        private class AsyncBlockManagedMemory : MemoryManager<byte>
        {
            private AsyncBlockAllocator _asyncBlockAllocator;
            private int _offset;
            private int _blockSize;

            public AsyncBlockManagedMemory(AsyncBlockAllocator asyncBlockAllocator, int offset, int blockSize)
            {
                _blockSize = blockSize;
                _asyncBlockAllocator = asyncBlockAllocator;
                _offset = offset;
            }

            public override Span<byte> GetSpan() => new Span<byte>(_asyncBlockAllocator._slab, _offset, _blockSize);

            public override Memory<byte> Memory => new Memory<byte>(_asyncBlockAllocator._slab, _offset, _blockSize);

            public unsafe override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle(_asyncBlockAllocator.GetPointer(_offset, elementIndex));

            public override void Unpin() { }

            protected override void Dispose(bool disposing)
            {
                _asyncBlockAllocator.Return(_offset);
            }
        }
    }
}
