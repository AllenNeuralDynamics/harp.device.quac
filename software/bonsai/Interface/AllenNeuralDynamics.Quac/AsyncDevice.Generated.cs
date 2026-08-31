using Bonsai.Harp;
using System.Threading;
using System.Threading.Tasks;

namespace AllenNeuralDynamics.Quac
{
    /// <inheritdoc/>
    public partial class Device
    {
        /// <summary>
        /// Initializes a new instance of the asynchronous API to configure and interface
        /// with Quac devices on the specified serial port.
        /// </summary>
        /// <param name="portName">
        /// The name of the serial port used to communicate with the Harp device.
        /// </param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous initialization operation. The value of
        /// the <see cref="Task{TResult}.Result"/> parameter contains a new instance of
        /// the <see cref="AsyncDevice"/> class.
        /// </returns>
        public static async Task<AsyncDevice> CreateAsync(string portName, CancellationToken cancellationToken = default)
        {
            var device = new AsyncDevice(portName);
            var whoAmI = await device.ReadWhoAmIAsync(cancellationToken);
            if (whoAmI != Device.WhoAmI)
            {
                var errorMessage = string.Format(
                    "The device ID {1} on {0} was unexpected. Check whether a Quac device is connected to the specified serial port.",
                    portName, whoAmI);
                throw new HarpException(errorMessage);
            }

            return device;
        }
    }

    /// <summary>
    /// Represents an asynchronous API to configure and interface with Quac devices.
    /// </summary>
    public partial class AsyncDevice : Bonsai.Harp.AsyncDevice
    {
        internal AsyncDevice(string portName)
            : base(portName)
        {
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DOPortState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalOutputs> ReadDOPortStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortState.Address), cancellationToken);
            return DOPortState.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DOPortState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalOutputs>> ReadTimestampedDOPortStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortState.Address), cancellationToken);
            return DOPortState.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DOPortState"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDOPortStateAsync(DigitalOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DOPortState.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DOPortSet"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalOutputs> ReadDOPortSetAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortSet.Address), cancellationToken);
            return DOPortSet.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DOPortSet"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalOutputs>> ReadTimestampedDOPortSetAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortSet.Address), cancellationToken);
            return DOPortSet.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DOPortSet"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDOPortSetAsync(DigitalOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DOPortSet.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DOPortClear"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalOutputs> ReadDOPortClearAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortClear.Address), cancellationToken);
            return DOPortClear.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DOPortClear"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalOutputs>> ReadTimestampedDOPortClearAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DOPortClear.Address), cancellationToken);
            return DOPortClear.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DOPortClear"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDOPortClearAsync(DigitalOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DOPortClear.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ExternalTriggerState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalInputs> ReadExternalTriggerStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ExternalTriggerState.Address), cancellationToken);
            return ExternalTriggerState.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ExternalTriggerState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalInputs>> ReadTimestampedExternalTriggerStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ExternalTriggerState.Address), cancellationToken);
            return ExternalTriggerState.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="AOPortState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AOPortStatePayload> ReadAOPortStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOPortState.Address), cancellationToken);
            return AOPortState.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="AOPortState"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AOPortStatePayload>> ReadTimestampedAOPortStateAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOPortState.Address), cancellationToken);
            return AOPortState.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="AOPortState"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteAOPortStateAsync(AOPortStatePayload value, CancellationToken cancellationToken = default)
        {
            var request = AOPortState.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="AOChannel0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<float> ReadAOChannel0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel0.Address), cancellationToken);
            return AOChannel0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="AOChannel0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<float>> ReadTimestampedAOChannel0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel0.Address), cancellationToken);
            return AOChannel0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="AOChannel0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteAOChannel0Async(float value, CancellationToken cancellationToken = default)
        {
            var request = AOChannel0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="AOChannel1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<float> ReadAOChannel1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel1.Address), cancellationToken);
            return AOChannel1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="AOChannel1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<float>> ReadTimestampedAOChannel1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel1.Address), cancellationToken);
            return AOChannel1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="AOChannel1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteAOChannel1Async(float value, CancellationToken cancellationToken = default)
        {
            var request = AOChannel1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="AOChannel2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<float> ReadAOChannel2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel2.Address), cancellationToken);
            return AOChannel2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="AOChannel2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<float>> ReadTimestampedAOChannel2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel2.Address), cancellationToken);
            return AOChannel2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="AOChannel2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteAOChannel2Async(float value, CancellationToken cancellationToken = default)
        {
            var request = AOChannel2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="AOChannel3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<float> ReadAOChannel3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel3.Address), cancellationToken);
            return AOChannel3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="AOChannel3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<float>> ReadTimestampedAOChannel3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadSingle(AOChannel3.Address), cancellationToken);
            return AOChannel3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="AOChannel3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteAOChannel3Async(float value, CancellationToken cancellationToken = default)
        {
            var request = AOChannel3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DacReady"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AnalogOutputs> ReadDacReadyAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacReady.Address), cancellationToken);
            return DacReady.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DacReady"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AnalogOutputs>> ReadTimestampedDacReadyAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacReady.Address), cancellationToken);
            return DacReady.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DacStart"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AnalogOutputs> ReadDacStartAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacStart.Address), cancellationToken);
            return DacStart.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DacStart"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AnalogOutputs>> ReadTimestampedDacStartAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacStart.Address), cancellationToken);
            return DacStart.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DacStart"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDacStartAsync(AnalogOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DacStart.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DacPause"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AnalogOutputs> ReadDacPauseAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacPause.Address), cancellationToken);
            return DacPause.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DacPause"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AnalogOutputs>> ReadTimestampedDacPauseAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacPause.Address), cancellationToken);
            return DacPause.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DacPause"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDacPauseAsync(AnalogOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DacPause.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DacAbort"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AnalogOutputs> ReadDacAbortAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacAbort.Address), cancellationToken);
            return DacAbort.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DacAbort"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AnalogOutputs>> ReadTimestampedDacAbortAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacAbort.Address), cancellationToken);
            return DacAbort.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="DacAbort"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteDacAbortAsync(AnalogOutputs value, CancellationToken cancellationToken = default)
        {
            var request = DacAbort.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="DacFinished"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<AnalogOutputs> ReadDacFinishedAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacFinished.Address), cancellationToken);
            return DacFinished.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="DacFinished"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<AnalogOutputs>> ReadTimestampedDacFinishedAsync(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(DacFinished.Address), cancellationToken);
            return DacFinished.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ChannelExternalTriggers0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalInputs> ReadChannelExternalTriggers0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers0.Address), cancellationToken);
            return ChannelExternalTriggers0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ChannelExternalTriggers0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalInputs>> ReadTimestampedChannelExternalTriggers0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers0.Address), cancellationToken);
            return ChannelExternalTriggers0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ChannelExternalTriggers0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteChannelExternalTriggers0Async(DigitalInputs value, CancellationToken cancellationToken = default)
        {
            var request = ChannelExternalTriggers0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ChannelExternalTriggers1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalInputs> ReadChannelExternalTriggers1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers1.Address), cancellationToken);
            return ChannelExternalTriggers1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ChannelExternalTriggers1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalInputs>> ReadTimestampedChannelExternalTriggers1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers1.Address), cancellationToken);
            return ChannelExternalTriggers1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ChannelExternalTriggers1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteChannelExternalTriggers1Async(DigitalInputs value, CancellationToken cancellationToken = default)
        {
            var request = ChannelExternalTriggers1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ChannelExternalTriggers2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalInputs> ReadChannelExternalTriggers2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers2.Address), cancellationToken);
            return ChannelExternalTriggers2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ChannelExternalTriggers2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalInputs>> ReadTimestampedChannelExternalTriggers2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers2.Address), cancellationToken);
            return ChannelExternalTriggers2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ChannelExternalTriggers2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteChannelExternalTriggers2Async(DigitalInputs value, CancellationToken cancellationToken = default)
        {
            var request = ChannelExternalTriggers2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ChannelExternalTriggers3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<DigitalInputs> ReadChannelExternalTriggers3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers3.Address), cancellationToken);
            return ChannelExternalTriggers3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ChannelExternalTriggers3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<DigitalInputs>> ReadTimestampedChannelExternalTriggers3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ChannelExternalTriggers3.Address), cancellationToken);
            return ChannelExternalTriggers3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ChannelExternalTriggers3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteChannelExternalTriggers3Async(DigitalInputs value, CancellationToken cancellationToken = default)
        {
            var request = ChannelExternalTriggers3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ActivePlayer0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<PlayerType> ReadActivePlayer0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer0.Address), cancellationToken);
            return ActivePlayer0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ActivePlayer0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<PlayerType>> ReadTimestampedActivePlayer0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer0.Address), cancellationToken);
            return ActivePlayer0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ActivePlayer0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteActivePlayer0Async(PlayerType value, CancellationToken cancellationToken = default)
        {
            var request = ActivePlayer0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ActivePlayer1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<PlayerType> ReadActivePlayer1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer1.Address), cancellationToken);
            return ActivePlayer1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ActivePlayer1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<PlayerType>> ReadTimestampedActivePlayer1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer1.Address), cancellationToken);
            return ActivePlayer1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ActivePlayer1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteActivePlayer1Async(PlayerType value, CancellationToken cancellationToken = default)
        {
            var request = ActivePlayer1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ActivePlayer2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<PlayerType> ReadActivePlayer2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer2.Address), cancellationToken);
            return ActivePlayer2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ActivePlayer2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<PlayerType>> ReadTimestampedActivePlayer2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer2.Address), cancellationToken);
            return ActivePlayer2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ActivePlayer2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteActivePlayer2Async(PlayerType value, CancellationToken cancellationToken = default)
        {
            var request = ActivePlayer2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="ActivePlayer3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<PlayerType> ReadActivePlayer3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer3.Address), cancellationToken);
            return ActivePlayer3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="ActivePlayer3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<PlayerType>> ReadTimestampedActivePlayer3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(ActivePlayer3.Address), cancellationToken);
            return ActivePlayer3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="ActivePlayer3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteActivePlayer3Async(PlayerType value, CancellationToken cancellationToken = default)
        {
            var request = ActivePlayer3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="FileSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<FileSettings0Payload> ReadFileSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings0.Address), cancellationToken);
            return FileSettings0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="FileSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<FileSettings0Payload>> ReadTimestampedFileSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings0.Address), cancellationToken);
            return FileSettings0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="FileSettings0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteFileSettings0Async(FileSettings0Payload value, CancellationToken cancellationToken = default)
        {
            var request = FileSettings0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="FileSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<FileSettings1Payload> ReadFileSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings1.Address), cancellationToken);
            return FileSettings1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="FileSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<FileSettings1Payload>> ReadTimestampedFileSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings1.Address), cancellationToken);
            return FileSettings1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="FileSettings1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteFileSettings1Async(FileSettings1Payload value, CancellationToken cancellationToken = default)
        {
            var request = FileSettings1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="FileSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<FileSettings2Payload> ReadFileSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings2.Address), cancellationToken);
            return FileSettings2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="FileSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<FileSettings2Payload>> ReadTimestampedFileSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings2.Address), cancellationToken);
            return FileSettings2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="FileSettings2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteFileSettings2Async(FileSettings2Payload value, CancellationToken cancellationToken = default)
        {
            var request = FileSettings2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="FileSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<FileSettings3Payload> ReadFileSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings3.Address), cancellationToken);
            return FileSettings3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="FileSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<FileSettings3Payload>> ReadTimestampedFileSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(FileSettings3.Address), cancellationToken);
            return FileSettings3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="FileSettings3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteFileSettings3Async(FileSettings3Payload value, CancellationToken cancellationToken = default)
        {
            var request = FileSettings3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="SineSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<SineSettings0Payload> ReadSineSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings0.Address), cancellationToken);
            return SineSettings0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="SineSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<SineSettings0Payload>> ReadTimestampedSineSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings0.Address), cancellationToken);
            return SineSettings0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="SineSettings0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteSineSettings0Async(SineSettings0Payload value, CancellationToken cancellationToken = default)
        {
            var request = SineSettings0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="SineSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<SineSettings1Payload> ReadSineSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings1.Address), cancellationToken);
            return SineSettings1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="SineSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<SineSettings1Payload>> ReadTimestampedSineSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings1.Address), cancellationToken);
            return SineSettings1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="SineSettings1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteSineSettings1Async(SineSettings1Payload value, CancellationToken cancellationToken = default)
        {
            var request = SineSettings1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="SineSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<SineSettings2Payload> ReadSineSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings2.Address), cancellationToken);
            return SineSettings2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="SineSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<SineSettings2Payload>> ReadTimestampedSineSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings2.Address), cancellationToken);
            return SineSettings2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="SineSettings2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteSineSettings2Async(SineSettings2Payload value, CancellationToken cancellationToken = default)
        {
            var request = SineSettings2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="SineSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<SineSettings3Payload> ReadSineSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings3.Address), cancellationToken);
            return SineSettings3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="SineSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<SineSettings3Payload>> ReadTimestampedSineSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(SineSettings3.Address), cancellationToken);
            return SineSettings3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="SineSettings3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteSineSettings3Async(SineSettings3Payload value, CancellationToken cancellationToken = default)
        {
            var request = SineSettings3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="TrapezoidSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<TrapezoidSettings0Payload> ReadTrapezoidSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings0.Address), cancellationToken);
            return TrapezoidSettings0.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="TrapezoidSettings0"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<TrapezoidSettings0Payload>> ReadTimestampedTrapezoidSettings0Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings0.Address), cancellationToken);
            return TrapezoidSettings0.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="TrapezoidSettings0"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteTrapezoidSettings0Async(TrapezoidSettings0Payload value, CancellationToken cancellationToken = default)
        {
            var request = TrapezoidSettings0.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="TrapezoidSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<TrapezoidSettings1Payload> ReadTrapezoidSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings1.Address), cancellationToken);
            return TrapezoidSettings1.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="TrapezoidSettings1"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<TrapezoidSettings1Payload>> ReadTimestampedTrapezoidSettings1Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings1.Address), cancellationToken);
            return TrapezoidSettings1.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="TrapezoidSettings1"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteTrapezoidSettings1Async(TrapezoidSettings1Payload value, CancellationToken cancellationToken = default)
        {
            var request = TrapezoidSettings1.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="TrapezoidSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<TrapezoidSettings2Payload> ReadTrapezoidSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings2.Address), cancellationToken);
            return TrapezoidSettings2.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="TrapezoidSettings2"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<TrapezoidSettings2Payload>> ReadTimestampedTrapezoidSettings2Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings2.Address), cancellationToken);
            return TrapezoidSettings2.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="TrapezoidSettings2"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteTrapezoidSettings2Async(TrapezoidSettings2Payload value, CancellationToken cancellationToken = default)
        {
            var request = TrapezoidSettings2.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the contents of the <see cref="TrapezoidSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the register payload.
        /// </returns>
        public async Task<TrapezoidSettings3Payload> ReadTrapezoidSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings3.Address), cancellationToken);
            return TrapezoidSettings3.GetPayload(reply);
        }

        /// <summary>
        /// Asynchronously reads the timestamped contents of the <see cref="TrapezoidSettings3"/> register.
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The task result contains
        /// the timestamped register payload.
        /// </returns>
        public async Task<Timestamped<TrapezoidSettings3Payload>> ReadTimestampedTrapezoidSettings3Async(CancellationToken cancellationToken = default)
        {
            var reply = await CommandAsync(HarpCommand.ReadByte(TrapezoidSettings3.Address), cancellationToken);
            return TrapezoidSettings3.GetTimestampedPayload(reply);
        }

        /// <summary>
        /// Asynchronously writes a value to the <see cref="TrapezoidSettings3"/> register.
        /// </summary>
        /// <param name="value">The value to write in the register.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> which can be used to cancel the operation.
        /// </param>
        /// <returns>The task object representing the asynchronous write operation.</returns>
        public async Task WriteTrapezoidSettings3Async(TrapezoidSettings3Payload value, CancellationToken cancellationToken = default)
        {
            var request = TrapezoidSettings3.FromPayload(MessageType.Write, value);
            await CommandAsync(request, cancellationToken);
        }
    }
}
