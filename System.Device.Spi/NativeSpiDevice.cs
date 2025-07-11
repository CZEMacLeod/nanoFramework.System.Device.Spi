﻿//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;

namespace System.Device.Spi
{
    /// <summary>
    /// The communications channel to a device on a SPI bus.
    /// </summary>
    internal sealed class NativeSpiDevice : SpiDevice
    {
        // generate a unique ID for the device by joining the SPI bus ID and the chip select line, should be pretty unique
        // the encoding is (SPI bus number x 1000 + chip select line number)
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private const int deviceUniqueIdMultiplier = 1000;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _deviceId;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Spi​Connection​Settings _connectionSettings;

        private bool _disposedValue;

        // this is used as the lock object 
        // a lock is required because multiple threads can access the device (Dispose)
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private object _syncLock;

        // For the ReadByte and WriteByte operations
        private readonly byte[] _bufferSingleOperation = new byte[1];

        /// <summary>
        /// The connection settings of a device on a SPI bus. The connection settings are immutable after the device is created
        /// so the object returned will be a clone of the settings object.
        /// </summary>
        public override SpiConnectionSettings ConnectionSettings
        {
            get => new SpiConnectionSettings(_connectionSettings);
        }

        /// <summary>
        /// Reads a byte from the SPI device.
        /// </summary>
        /// <returns>A byte read from the SPI device.</returns>
        public override byte ReadByte()
        {
            NativeTransfer(null, _bufferSingleOperation, false);
            return _bufferSingleOperation[0];
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the data from the SPI device.
        /// The length of the buffer determines how much data to read from the SPI device.
        /// </param>
        public override void Read(SpanByte buffer)
        {
            NativeTransfer(null, buffer, false);
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the data from the SPI device.
        /// The length of the buffer determines how much data to read from the SPI device.
        /// </param>
        public override void Read(ushort[] buffer)
        {
            NativeTransfer(null, buffer, false);
        }

        /// <summary>
        /// Writes a byte to the SPI device.
        /// </summary>
        /// <param name="value">The byte to be written to the SPI device.</param>
        public override void WriteByte(byte value)
        {
            _bufferSingleOperation[0] = value;
            NativeTransfer(_bufferSingleOperation, null, false);
        }

        /// <summary>
        /// Writes data to the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that contains the data to be written to the SPI device.
        /// </param>
        public override void Write(ushort[] buffer)
        {
            NativeTransfer(buffer, null, false);
        }

        /// <summary>
        /// Writes data to the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that contains the data to be written to the SPI device.
        /// </param>
        public override void Write(SpanByte buffer)
        {
            NativeTransfer(buffer, null, false);
        }

        /// <summary>
        /// Writes and reads data from the SPI device.
        /// </summary>
        /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
        /// <param name="readBuffer">The buffer to read the data from the SPI device.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="ConnectionSettings"/> for this <see cref="SpiDevice"/> aren't configured for <see cref="SpiBusConfiguration.FullDuplex"/>.</exception>
        public override void TransferFullDuplex(
            ushort[] writeBuffer,
            ushort[] readBuffer)
        {
            if (_connectionSettings.Configuration != SpiBusConfiguration.FullDuplex)
            {
                throw new InvalidOperationException();
            }

            NativeTransfer(writeBuffer, readBuffer, true);
        }

        /// <summary>
        /// Writes and reads data from the SPI device.
        /// </summary>
        /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
        /// <param name="readBuffer">The buffer to read the data from the SPI device.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="ConnectionSettings"/> for this <see cref="SpiDevice"/> aren't configured for <see cref="SpiBusConfiguration.FullDuplex"/>.</exception>
        public override void TransferFullDuplex(
            SpanByte writeBuffer,
            SpanByte readBuffer)
        {
            if (_connectionSettings.Configuration != SpiBusConfiguration.FullDuplex)
            {
                throw new InvalidOperationException();
            }

            NativeTransfer(writeBuffer, readBuffer, true);
        }

        /// <summary>
        /// Creates a communications channel to a device on a SPI bus running on the current hardware.
        /// </summary>
        /// <param name="settings">The connection settings of a device on a SPI bus.</param>
        /// <exception cref="ArgumentException">
        /// <para><see cref="SpiConnectionSettings.ChipSelectLine"/> is not valid.</para>
        /// <para>- or -</para>
        /// <para>The specified <see cref="SpiConnectionSettings.BusId"/> is not available.</para>
        /// <para>- or -</para>
        /// <para>One, or more of the GPIOs for the SPI bus are already used.</para>
        /// <para>- or -</para>
        /// <para>Some other invalid property in the specified <see cref="SpiConnectionSettings"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Maximum number of SPI devices for the specified bus has been reached.</exception>
        /// <exception cref="SpiDeviceAlreadyInUseException">If <see cref="SpiConnectionSettings.ChipSelectLine"/> it's already in use.</exception>
        internal NativeSpiDevice(SpiConnectionSettings settings)
        {
            try
            {
                _connectionSettings = new SpiConnectionSettings(settings);

                _deviceId = NativeOpenDevice();
            }
            catch (NotSupportedException)
            {
                // NotSupportedException 
                //   Device(chip select) already in use
                throw new SpiDeviceAlreadyInUseException();
            }
            // these can also be thrown bt the native driver
            // ArgumentException
            //   Invalid port or unable to init bus
            // IndexOutOfRangeException
            //   Too many devices open or spi already in use

            // device doesn't exist, create it...
            _connectionSettings = new SpiConnectionSettings(settings);

            _syncLock = new object();
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if explicitly disposing, <see langword="false"/> if in finalizer</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                lock (_syncLock)
                {
                    if (!_disposedValue)
                    {
                        DisposeNative();

                        _disposedValue = true;
                    }
                }
            }
        }

        #region Native Calls

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void DisposeNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeTransfer(SpanByte writeBuffer, SpanByte readBuffer, bool fullDuplex);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeTransfer(ushort[] writeBuffer, ushort[] readBuffer, bool fullDuplex);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeInit();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern Int32 NativeOpenDevice();

        #endregion
    }
}
