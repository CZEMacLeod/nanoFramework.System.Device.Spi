//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace System.Device.Spi
{
    /// <summary>
    /// The communications channel to a device on a SPI bus.
    /// </summary>
    public abstract class SpiDevice : IDisposable
    {
        /// <summary>
        /// The connection settings of a device on a SPI bus. The connection settings are immutable after the device is created
        /// so the object returned will be a clone of the settings object.
        /// </summary>
        public abstract SpiConnectionSettings ConnectionSettings { get; }

        /// <summary>
        /// Reads a byte from the SPI device.
        /// </summary>
        /// <returns>A byte read from the SPI device.</returns>
        public virtual byte ReadByte()
        {
            byte[] buffer = new byte[1] { 0 };
            SpanByte spanByte = new SpanByte(buffer);
            Read(spanByte);
            return buffer[0];
        }

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the data from the SPI device.
        /// The length of the buffer determines how much data to read from the SPI device.
        /// </param>
        public abstract void Read(SpanByte buffer);

        /// <summary>
        /// Reads data from the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to read the data from the SPI device.
        /// The length of the buffer determines how much data to read from the SPI device.
        /// </param>
        public abstract void Read(ushort[] buffer);

        /// <summary>
        /// Writes a byte to the SPI device.
        /// </summary>
        /// <param name="value">The byte to be written to the SPI device.</param>
        public virtual void WriteByte(byte value)
        {
            byte[] buffer = new byte[1] { value };
            SpanByte spanByte = new SpanByte(buffer);
            Write(spanByte);
        }

        /// <summary>
        /// Writes data to the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that contains the data to be written to the SPI device.
        /// </param>
        public abstract void Write(ushort[] buffer);

        /// <summary>
        /// Writes data to the SPI device.
        /// </summary>
        /// <param name="buffer">
        /// The buffer that contains the data to be written to the SPI device.
        /// </param>
        public abstract void Write(SpanByte buffer);

        /// <summary>
        /// Writes and reads data from the SPI device.
        /// </summary>
        /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
        /// <param name="readBuffer">The buffer to read the data from the SPI device.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="ConnectionSettings"/> for this <see cref="SpiDevice"/> aren't configured for <see cref="SpiBusConfiguration.FullDuplex"/>.</exception>
        public abstract void TransferFullDuplex(
            ushort[] writeBuffer,
            ushort[] readBuffer);

        /// <summary>
        /// Writes and reads data from the SPI device.
        /// </summary>
        /// <param name="writeBuffer">The buffer that contains the data to be written to the SPI device.</param>
        /// <param name="readBuffer">The buffer to read the data from the SPI device.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="ConnectionSettings"/> for this <see cref="SpiDevice"/> aren't configured for <see cref="SpiBusConfiguration.FullDuplex"/>.</exception>
        public abstract void TransferFullDuplex(
            SpanByte writeBuffer,
            SpanByte readBuffer);

        /// <summary>
        /// Retrieves the info about a certain bus.
        /// </summary>
        /// <param name="busId">The id of the bus.</param>
        /// <returns>The bus info requested.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="busId"/> is not available in the device.</exception>
        public static SpiBusInfo GetBusInfo(int busId)
        {
            return new SpiBusInfo(busId);
        }

        /// <summary>
        /// Creates a communications channel to a device on a SPI bus running on the current hardware.
        /// </summary>
        /// <param name="settings">The connection settings of a device on a SPI bus.</param>
        /// <returns>A communications channel to a device on a SPI bus.</returns>
        public static SpiDevice Create(SpiConnectionSettings settings)
        {
            return new NativeSpiDevice(settings);
        }


        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if explicitly disposing, <see langword="false"/> if in finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc/>
        ~SpiDevice()
        {
            Dispose(false);
        }
    }
}
