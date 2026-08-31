using Bonsai;
using Bonsai.Harp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Serialization;

namespace AllenNeuralDynamics.Quac
{
    /// <summary>
    /// Generates events and processes commands for the Quac device connected
    /// at the specified serial port.
    /// </summary>
    [Combinator(MethodName = nameof(Generate))]
    [WorkflowElementCategory(ElementCategory.Source)]
    [Description("Generates events and processes commands for the Quac device.")]
    public partial class Device : Bonsai.Harp.Device, INamedElement
    {
        /// <summary>
        /// Represents the unique identity class of the <see cref="Quac"/> device.
        /// This field is constant.
        /// </summary>
        public const int WhoAmI = 1411;

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        public Device() : base(WhoAmI) { }

        string INamedElement.Name => nameof(Quac);

        /// <summary>
        /// Gets a read-only mapping from address to register type.
        /// </summary>
        public static new IReadOnlyDictionary<int, Type> RegisterMap { get; } = new Dictionary<int, Type>
            (Bonsai.Harp.Device.RegisterMap.ToDictionary(entry => entry.Key, entry => entry.Value))
        {
            { 32, typeof(DOPortState) },
            { 33, typeof(DOPortSet) },
            { 34, typeof(DOPortClear) },
            { 35, typeof(ExternalTriggerState) },
            { 36, typeof(AOPortState) },
            { 37, typeof(AOChannel0) },
            { 38, typeof(AOChannel1) },
            { 39, typeof(AOChannel2) },
            { 40, typeof(AOChannel3) },
            { 41, typeof(DacReady) },
            { 42, typeof(DacStart) },
            { 43, typeof(DacPause) },
            { 44, typeof(DacAbort) },
            { 45, typeof(DacFinished) },
            { 46, typeof(ChannelExternalTriggers0) },
            { 47, typeof(ChannelExternalTriggers1) },
            { 48, typeof(ChannelExternalTriggers2) },
            { 49, typeof(ChannelExternalTriggers3) },
            { 50, typeof(ActivePlayer0) },
            { 51, typeof(ActivePlayer1) },
            { 52, typeof(ActivePlayer2) },
            { 53, typeof(ActivePlayer3) },
            { 54, typeof(FileSettings0) },
            { 55, typeof(FileSettings1) },
            { 56, typeof(FileSettings2) },
            { 57, typeof(FileSettings3) },
            { 58, typeof(SineSettings0) },
            { 59, typeof(SineSettings1) },
            { 60, typeof(SineSettings2) },
            { 61, typeof(SineSettings3) },
            { 62, typeof(TrapezoidSettings0) },
            { 63, typeof(TrapezoidSettings1) },
            { 64, typeof(TrapezoidSettings2) },
            { 65, typeof(TrapezoidSettings3) }
        };

        /// <summary>
        /// Gets the contents of the metadata file describing the <see cref="Quac"/>
        /// device registers.
        /// </summary>
        public static readonly string Metadata = GetDeviceMetadata();

        static string GetDeviceMetadata()
        {
            var deviceType = typeof(Device);
            using var metadataStream = deviceType.Assembly.GetManifestResourceStream($"{deviceType.Namespace}.device.yml");
            using var streamReader = new System.IO.StreamReader(metadataStream);
            return streamReader.ReadToEnd();
        }
    }

    /// <summary>
    /// Represents an operator that returns the contents of the metadata file
    /// describing the <see cref="Quac"/> device registers.
    /// </summary>
    [Description("Returns the contents of the metadata file describing the Quac device registers.")]
    public partial class GetDeviceMetadata : Source<string>
    {
        /// <summary>
        /// Returns an observable sequence with the contents of the metadata file
        /// describing the <see cref="Quac"/> device registers.
        /// </summary>
        /// <returns>
        /// A sequence with a single <see cref="string"/> object representing the
        /// contents of the metadata file.
        /// </returns>
        public override IObservable<string> Generate()
        {
            return Observable.Return(Device.Metadata);
        }
    }

    /// <summary>
    /// Represents an operator that groups the sequence of <see cref="Quac"/>" messages by register type.
    /// </summary>
    [Description("Groups the sequence of Quac messages by register type.")]
    public partial class GroupByRegister : Combinator<HarpMessage, IGroupedObservable<Type, HarpMessage>>
    {
        /// <summary>
        /// Groups an observable sequence of <see cref="Quac"/> messages
        /// by register type.
        /// </summary>
        /// <param name="source">The sequence of Harp device messages.</param>
        /// <returns>
        /// A sequence of observable groups, each of which corresponds to a unique
        /// <see cref="Quac"/> register.
        /// </returns>
        public override IObservable<IGroupedObservable<Type, HarpMessage>> Process(IObservable<HarpMessage> source)
        {
            return source.GroupBy(message => Device.RegisterMap[message.Address]);
        }
    }

    /// <summary>
    /// Represents an operator that writes the sequence of <see cref="Quac"/>" messages
    /// to the standard Harp storage format.
    /// </summary>
    [DefaultProperty(nameof(Path))]
    [Description("Writes the sequence of Quac messages to the standard Harp storage format.")]
    public partial class DeviceDataWriter : Sink<HarpMessage>, INamedElement
    {
        const string BinaryExtension = ".bin";
        const string MetadataFileName = "device.yml";
        readonly Bonsai.Harp.MessageWriter writer = new();

        string INamedElement.Name => nameof(Quac) + "DataWriter";

        /// <summary>
        /// Gets or sets the relative or absolute path on which to save the message data.
        /// </summary>
        [Description("The relative or absolute path of the directory on which to save the message data.")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        public string Path
        {
            get => System.IO.Path.GetDirectoryName(writer.FileName);
            set => writer.FileName = System.IO.Path.Combine(value, nameof(Quac) + BinaryExtension);
        }

        /// <summary>
        /// Gets or sets a value indicating whether element writing should be buffered. If <see langword="true"/>,
        /// the write commands will be queued in memory as fast as possible and will be processed
        /// by the writer in a different thread. Otherwise, writing will be done in the same
        /// thread in which notifications arrive.
        /// </summary>
        [Description("Indicates whether writing should be buffered.")]
        public bool Buffered
        {
            get => writer.Buffered;
            set => writer.Buffered = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output file if it already exists.")]
        public bool Overwrite
        {
            get => writer.Overwrite;
            set => writer.Overwrite = value;
        }

        /// <summary>
        /// Gets or sets a value specifying how the message filter will use the matching criteria.
        /// </summary>
        [Description("Specifies how the message filter will use the matching criteria.")]
        public FilterType FilterType
        {
            get => writer.FilterType;
            set => writer.FilterType = value;
        }

        /// <summary>
        /// Gets or sets a value specifying the expected message type. If no value is
        /// specified, all messages will be accepted.
        /// </summary>
        [Description("Specifies the expected message type. If no value is specified, all messages will be accepted.")]
        public MessageType? MessageType
        {
            get => writer.MessageType;
            set => writer.MessageType = value;
        }

        private IObservable<TSource> WriteDeviceMetadata<TSource>(IObservable<TSource> source)
        {
            var basePath = Path;
            if (string.IsNullOrEmpty(basePath))
                return source;

            var metadataPath = System.IO.Path.Combine(basePath, MetadataFileName);
            return Observable.Create<TSource>(observer =>
            {
                Bonsai.IO.PathHelper.EnsureDirectory(metadataPath);
                if (System.IO.File.Exists(metadataPath) && !Overwrite)
                {
                    throw new System.IO.IOException(string.Format("The file '{0}' already exists.", metadataPath));
                }

                System.IO.File.WriteAllText(metadataPath, Device.Metadata);
                return source.SubscribeSafe(observer);
            });
        }

        /// <summary>
        /// Writes each Harp message in the sequence to the specified binary file, and the
        /// contents of the device metadata file to a separate text file.
        /// </summary>
        /// <param name="source">The sequence of messages to write to the file.</param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the
        /// messages to a raw binary file, and the contents of the device metadata file
        /// to a separate text file.
        /// </returns>
        public override IObservable<HarpMessage> Process(IObservable<HarpMessage> source)
        {
            return source.Publish(ps => ps.Merge(
                WriteDeviceMetadata(writer.Process(ps.GroupBy(message => message.Address)))
                .IgnoreElements()
                .Cast<HarpMessage>()));
        }

        /// <summary>
        /// Writes each Harp message in the sequence of observable groups to the
        /// corresponding binary file, where the name of each file is generated from
        /// the common group register address. The contents of the device metadata file are
        /// written to a separate text file.
        /// </summary>
        /// <param name="source">
        /// A sequence of observable groups, each of which corresponds to a unique register
        /// address.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the Harp
        /// messages in each group to the corresponding file, and the contents of the device
        /// metadata file to a separate text file.
        /// </returns>
        public IObservable<IGroupedObservable<int, HarpMessage>> Process(IObservable<IGroupedObservable<int, HarpMessage>> source)
        {
            return WriteDeviceMetadata(writer.Process(source));
        }

        /// <summary>
        /// Writes each Harp message in the sequence of observable groups to the
        /// corresponding binary file, where the name of each file is generated from
        /// the common group register name. The contents of the device metadata file are
        /// written to a separate text file.
        /// </summary>
        /// <param name="source">
        /// A sequence of observable groups, each of which corresponds to a unique register
        /// type.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the Harp
        /// messages in each group to the corresponding file, and the contents of the device
        /// metadata file to a separate text file.
        /// </returns>
        public IObservable<IGroupedObservable<Type, HarpMessage>> Process(IObservable<IGroupedObservable<Type, HarpMessage>> source)
        {
            return WriteDeviceMetadata(writer.Process(source));
        }
    }

    /// <summary>
    /// Represents an operator that filters register-specific messages
    /// reported by the <see cref="Quac"/> device.
    /// </summary>
    /// <seealso cref="DOPortState"/>
    /// <seealso cref="DOPortSet"/>
    /// <seealso cref="DOPortClear"/>
    /// <seealso cref="ExternalTriggerState"/>
    /// <seealso cref="AOPortState"/>
    /// <seealso cref="AOChannel0"/>
    /// <seealso cref="AOChannel1"/>
    /// <seealso cref="AOChannel2"/>
    /// <seealso cref="AOChannel3"/>
    /// <seealso cref="DacReady"/>
    /// <seealso cref="DacStart"/>
    /// <seealso cref="DacPause"/>
    /// <seealso cref="DacAbort"/>
    /// <seealso cref="DacFinished"/>
    /// <seealso cref="ChannelExternalTriggers0"/>
    /// <seealso cref="ChannelExternalTriggers1"/>
    /// <seealso cref="ChannelExternalTriggers2"/>
    /// <seealso cref="ChannelExternalTriggers3"/>
    /// <seealso cref="ActivePlayer0"/>
    /// <seealso cref="ActivePlayer1"/>
    /// <seealso cref="ActivePlayer2"/>
    /// <seealso cref="ActivePlayer3"/>
    /// <seealso cref="FileSettings0"/>
    /// <seealso cref="FileSettings1"/>
    /// <seealso cref="FileSettings2"/>
    /// <seealso cref="FileSettings3"/>
    /// <seealso cref="SineSettings0"/>
    /// <seealso cref="SineSettings1"/>
    /// <seealso cref="SineSettings2"/>
    /// <seealso cref="SineSettings3"/>
    /// <seealso cref="TrapezoidSettings0"/>
    /// <seealso cref="TrapezoidSettings1"/>
    /// <seealso cref="TrapezoidSettings2"/>
    /// <seealso cref="TrapezoidSettings3"/>
    [XmlInclude(typeof(DOPortState))]
    [XmlInclude(typeof(DOPortSet))]
    [XmlInclude(typeof(DOPortClear))]
    [XmlInclude(typeof(ExternalTriggerState))]
    [XmlInclude(typeof(AOPortState))]
    [XmlInclude(typeof(AOChannel0))]
    [XmlInclude(typeof(AOChannel1))]
    [XmlInclude(typeof(AOChannel2))]
    [XmlInclude(typeof(AOChannel3))]
    [XmlInclude(typeof(DacReady))]
    [XmlInclude(typeof(DacStart))]
    [XmlInclude(typeof(DacPause))]
    [XmlInclude(typeof(DacAbort))]
    [XmlInclude(typeof(DacFinished))]
    [XmlInclude(typeof(ChannelExternalTriggers0))]
    [XmlInclude(typeof(ChannelExternalTriggers1))]
    [XmlInclude(typeof(ChannelExternalTriggers2))]
    [XmlInclude(typeof(ChannelExternalTriggers3))]
    [XmlInclude(typeof(ActivePlayer0))]
    [XmlInclude(typeof(ActivePlayer1))]
    [XmlInclude(typeof(ActivePlayer2))]
    [XmlInclude(typeof(ActivePlayer3))]
    [XmlInclude(typeof(FileSettings0))]
    [XmlInclude(typeof(FileSettings1))]
    [XmlInclude(typeof(FileSettings2))]
    [XmlInclude(typeof(FileSettings3))]
    [XmlInclude(typeof(SineSettings0))]
    [XmlInclude(typeof(SineSettings1))]
    [XmlInclude(typeof(SineSettings2))]
    [XmlInclude(typeof(SineSettings3))]
    [XmlInclude(typeof(TrapezoidSettings0))]
    [XmlInclude(typeof(TrapezoidSettings1))]
    [XmlInclude(typeof(TrapezoidSettings2))]
    [XmlInclude(typeof(TrapezoidSettings3))]
    [Description("Filters register-specific messages reported by the Quac device.")]
    public class FilterRegister : FilterRegisterBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRegister"/> class.
        /// </summary>
        public FilterRegister()
        {
            Register = new DOPortState();
        }

        string INamedElement.Name
        {
            get => $"{nameof(Quac)}.{GetElementDisplayName(Register)}";
        }
    }

    /// <summary>
    /// Represents an operator which filters and selects specific messages
    /// reported by the Quac device.
    /// </summary>
    /// <seealso cref="DOPortState"/>
    /// <seealso cref="DOPortSet"/>
    /// <seealso cref="DOPortClear"/>
    /// <seealso cref="ExternalTriggerState"/>
    /// <seealso cref="AOPortState"/>
    /// <seealso cref="AOChannel0"/>
    /// <seealso cref="AOChannel1"/>
    /// <seealso cref="AOChannel2"/>
    /// <seealso cref="AOChannel3"/>
    /// <seealso cref="DacReady"/>
    /// <seealso cref="DacStart"/>
    /// <seealso cref="DacPause"/>
    /// <seealso cref="DacAbort"/>
    /// <seealso cref="DacFinished"/>
    /// <seealso cref="ChannelExternalTriggers0"/>
    /// <seealso cref="ChannelExternalTriggers1"/>
    /// <seealso cref="ChannelExternalTriggers2"/>
    /// <seealso cref="ChannelExternalTriggers3"/>
    /// <seealso cref="ActivePlayer0"/>
    /// <seealso cref="ActivePlayer1"/>
    /// <seealso cref="ActivePlayer2"/>
    /// <seealso cref="ActivePlayer3"/>
    /// <seealso cref="FileSettings0"/>
    /// <seealso cref="FileSettings1"/>
    /// <seealso cref="FileSettings2"/>
    /// <seealso cref="FileSettings3"/>
    /// <seealso cref="SineSettings0"/>
    /// <seealso cref="SineSettings1"/>
    /// <seealso cref="SineSettings2"/>
    /// <seealso cref="SineSettings3"/>
    /// <seealso cref="TrapezoidSettings0"/>
    /// <seealso cref="TrapezoidSettings1"/>
    /// <seealso cref="TrapezoidSettings2"/>
    /// <seealso cref="TrapezoidSettings3"/>
    [XmlInclude(typeof(DOPortState))]
    [XmlInclude(typeof(DOPortSet))]
    [XmlInclude(typeof(DOPortClear))]
    [XmlInclude(typeof(ExternalTriggerState))]
    [XmlInclude(typeof(AOPortState))]
    [XmlInclude(typeof(AOChannel0))]
    [XmlInclude(typeof(AOChannel1))]
    [XmlInclude(typeof(AOChannel2))]
    [XmlInclude(typeof(AOChannel3))]
    [XmlInclude(typeof(DacReady))]
    [XmlInclude(typeof(DacStart))]
    [XmlInclude(typeof(DacPause))]
    [XmlInclude(typeof(DacAbort))]
    [XmlInclude(typeof(DacFinished))]
    [XmlInclude(typeof(ChannelExternalTriggers0))]
    [XmlInclude(typeof(ChannelExternalTriggers1))]
    [XmlInclude(typeof(ChannelExternalTriggers2))]
    [XmlInclude(typeof(ChannelExternalTriggers3))]
    [XmlInclude(typeof(ActivePlayer0))]
    [XmlInclude(typeof(ActivePlayer1))]
    [XmlInclude(typeof(ActivePlayer2))]
    [XmlInclude(typeof(ActivePlayer3))]
    [XmlInclude(typeof(FileSettings0))]
    [XmlInclude(typeof(FileSettings1))]
    [XmlInclude(typeof(FileSettings2))]
    [XmlInclude(typeof(FileSettings3))]
    [XmlInclude(typeof(SineSettings0))]
    [XmlInclude(typeof(SineSettings1))]
    [XmlInclude(typeof(SineSettings2))]
    [XmlInclude(typeof(SineSettings3))]
    [XmlInclude(typeof(TrapezoidSettings0))]
    [XmlInclude(typeof(TrapezoidSettings1))]
    [XmlInclude(typeof(TrapezoidSettings2))]
    [XmlInclude(typeof(TrapezoidSettings3))]
    [XmlInclude(typeof(TimestampedDOPortState))]
    [XmlInclude(typeof(TimestampedDOPortSet))]
    [XmlInclude(typeof(TimestampedDOPortClear))]
    [XmlInclude(typeof(TimestampedExternalTriggerState))]
    [XmlInclude(typeof(TimestampedAOPortState))]
    [XmlInclude(typeof(TimestampedAOChannel0))]
    [XmlInclude(typeof(TimestampedAOChannel1))]
    [XmlInclude(typeof(TimestampedAOChannel2))]
    [XmlInclude(typeof(TimestampedAOChannel3))]
    [XmlInclude(typeof(TimestampedDacReady))]
    [XmlInclude(typeof(TimestampedDacStart))]
    [XmlInclude(typeof(TimestampedDacPause))]
    [XmlInclude(typeof(TimestampedDacAbort))]
    [XmlInclude(typeof(TimestampedDacFinished))]
    [XmlInclude(typeof(TimestampedChannelExternalTriggers0))]
    [XmlInclude(typeof(TimestampedChannelExternalTriggers1))]
    [XmlInclude(typeof(TimestampedChannelExternalTriggers2))]
    [XmlInclude(typeof(TimestampedChannelExternalTriggers3))]
    [XmlInclude(typeof(TimestampedActivePlayer0))]
    [XmlInclude(typeof(TimestampedActivePlayer1))]
    [XmlInclude(typeof(TimestampedActivePlayer2))]
    [XmlInclude(typeof(TimestampedActivePlayer3))]
    [XmlInclude(typeof(TimestampedFileSettings0))]
    [XmlInclude(typeof(TimestampedFileSettings1))]
    [XmlInclude(typeof(TimestampedFileSettings2))]
    [XmlInclude(typeof(TimestampedFileSettings3))]
    [XmlInclude(typeof(TimestampedSineSettings0))]
    [XmlInclude(typeof(TimestampedSineSettings1))]
    [XmlInclude(typeof(TimestampedSineSettings2))]
    [XmlInclude(typeof(TimestampedSineSettings3))]
    [XmlInclude(typeof(TimestampedTrapezoidSettings0))]
    [XmlInclude(typeof(TimestampedTrapezoidSettings1))]
    [XmlInclude(typeof(TimestampedTrapezoidSettings2))]
    [XmlInclude(typeof(TimestampedTrapezoidSettings3))]
    [Description("Filters and selects specific messages reported by the Quac device.")]
    public partial class Parse : ParseBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parse"/> class.
        /// </summary>
        public Parse()
        {
            Register = new DOPortState();
        }

        string INamedElement.Name => $"{nameof(Quac)}.{GetElementDisplayName(Register)}";
    }

    /// <summary>
    /// Represents an operator which formats a sequence of values as specific
    /// Quac register messages.
    /// </summary>
    /// <seealso cref="DOPortState"/>
    /// <seealso cref="DOPortSet"/>
    /// <seealso cref="DOPortClear"/>
    /// <seealso cref="ExternalTriggerState"/>
    /// <seealso cref="AOPortState"/>
    /// <seealso cref="AOChannel0"/>
    /// <seealso cref="AOChannel1"/>
    /// <seealso cref="AOChannel2"/>
    /// <seealso cref="AOChannel3"/>
    /// <seealso cref="DacReady"/>
    /// <seealso cref="DacStart"/>
    /// <seealso cref="DacPause"/>
    /// <seealso cref="DacAbort"/>
    /// <seealso cref="DacFinished"/>
    /// <seealso cref="ChannelExternalTriggers0"/>
    /// <seealso cref="ChannelExternalTriggers1"/>
    /// <seealso cref="ChannelExternalTriggers2"/>
    /// <seealso cref="ChannelExternalTriggers3"/>
    /// <seealso cref="ActivePlayer0"/>
    /// <seealso cref="ActivePlayer1"/>
    /// <seealso cref="ActivePlayer2"/>
    /// <seealso cref="ActivePlayer3"/>
    /// <seealso cref="FileSettings0"/>
    /// <seealso cref="FileSettings1"/>
    /// <seealso cref="FileSettings2"/>
    /// <seealso cref="FileSettings3"/>
    /// <seealso cref="SineSettings0"/>
    /// <seealso cref="SineSettings1"/>
    /// <seealso cref="SineSettings2"/>
    /// <seealso cref="SineSettings3"/>
    /// <seealso cref="TrapezoidSettings0"/>
    /// <seealso cref="TrapezoidSettings1"/>
    /// <seealso cref="TrapezoidSettings2"/>
    /// <seealso cref="TrapezoidSettings3"/>
    [XmlInclude(typeof(DOPortState))]
    [XmlInclude(typeof(DOPortSet))]
    [XmlInclude(typeof(DOPortClear))]
    [XmlInclude(typeof(ExternalTriggerState))]
    [XmlInclude(typeof(AOPortState))]
    [XmlInclude(typeof(AOChannel0))]
    [XmlInclude(typeof(AOChannel1))]
    [XmlInclude(typeof(AOChannel2))]
    [XmlInclude(typeof(AOChannel3))]
    [XmlInclude(typeof(DacReady))]
    [XmlInclude(typeof(DacStart))]
    [XmlInclude(typeof(DacPause))]
    [XmlInclude(typeof(DacAbort))]
    [XmlInclude(typeof(DacFinished))]
    [XmlInclude(typeof(ChannelExternalTriggers0))]
    [XmlInclude(typeof(ChannelExternalTriggers1))]
    [XmlInclude(typeof(ChannelExternalTriggers2))]
    [XmlInclude(typeof(ChannelExternalTriggers3))]
    [XmlInclude(typeof(ActivePlayer0))]
    [XmlInclude(typeof(ActivePlayer1))]
    [XmlInclude(typeof(ActivePlayer2))]
    [XmlInclude(typeof(ActivePlayer3))]
    [XmlInclude(typeof(FileSettings0))]
    [XmlInclude(typeof(FileSettings1))]
    [XmlInclude(typeof(FileSettings2))]
    [XmlInclude(typeof(FileSettings3))]
    [XmlInclude(typeof(SineSettings0))]
    [XmlInclude(typeof(SineSettings1))]
    [XmlInclude(typeof(SineSettings2))]
    [XmlInclude(typeof(SineSettings3))]
    [XmlInclude(typeof(TrapezoidSettings0))]
    [XmlInclude(typeof(TrapezoidSettings1))]
    [XmlInclude(typeof(TrapezoidSettings2))]
    [XmlInclude(typeof(TrapezoidSettings3))]
    [Description("Formats a sequence of values as specific Quac register messages.")]
    public partial class Format : FormatBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Format"/> class.
        /// </summary>
        public Format()
        {
            Register = new DOPortState();
        }

        string INamedElement.Name => $"{nameof(Quac)}.{GetElementDisplayName(Register)}";
    }

    /// <summary>
    /// Represents a register that reflects and specifies the state of the digital output lines.
    /// </summary>
    [Description("Reflects and specifies the state of the digital output lines.")]
    public partial class DOPortState
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortState"/> register. This field is constant.
        /// </summary>
        public const int Address = 32;

        /// <summary>
        /// Represents the payload type of the <see cref="DOPortState"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DOPortState"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalOutputs GetPayload(HarpMessage message)
        {
            return (DigitalOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DOPortState"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortState"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DOPortState"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortState"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DOPortState register.
    /// </summary>
    /// <seealso cref="DOPortState"/>
    [Description("Filters and selects timestamped messages from the DOPortState register.")]
    public partial class TimestampedDOPortState
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortState"/> register. This field is constant.
        /// </summary>
        public const int Address = DOPortState.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetPayload(HarpMessage message)
        {
            return DOPortState.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that sets the digital output lines specified in the mask to logic HIGH.
    /// </summary>
    [Description("Sets the digital output lines specified in the mask to logic HIGH.")]
    public partial class DOPortSet
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortSet"/> register. This field is constant.
        /// </summary>
        public const int Address = 33;

        /// <summary>
        /// Represents the payload type of the <see cref="DOPortSet"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DOPortSet"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DOPortSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalOutputs GetPayload(HarpMessage message)
        {
            return (DigitalOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DOPortSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DOPortSet"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortSet"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DOPortSet"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortSet"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DOPortSet register.
    /// </summary>
    /// <seealso cref="DOPortSet"/>
    [Description("Filters and selects timestamped messages from the DOPortSet register.")]
    public partial class TimestampedDOPortSet
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortSet"/> register. This field is constant.
        /// </summary>
        public const int Address = DOPortSet.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DOPortSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetPayload(HarpMessage message)
        {
            return DOPortSet.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that clears the digital output lines specified in the mask to logic LOW.
    /// </summary>
    [Description("Clears the digital output lines specified in the mask to logic LOW.")]
    public partial class DOPortClear
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortClear"/> register. This field is constant.
        /// </summary>
        public const int Address = 34;

        /// <summary>
        /// Represents the payload type of the <see cref="DOPortClear"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DOPortClear"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DOPortClear"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalOutputs GetPayload(HarpMessage message)
        {
            return (DigitalOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DOPortClear"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DOPortClear"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortClear"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DOPortClear"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DOPortClear"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DOPortClear register.
    /// </summary>
    /// <seealso cref="DOPortClear"/>
    [Description("Filters and selects timestamped messages from the DOPortClear register.")]
    public partial class TimestampedDOPortClear
    {
        /// <summary>
        /// Represents the address of the <see cref="DOPortClear"/> register. This field is constant.
        /// </summary>
        public const int Address = DOPortClear.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DOPortClear"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalOutputs> GetPayload(HarpMessage message)
        {
            return DOPortClear.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects the raw state of the external trigger input lines.
    /// </summary>
    [Description("Reflects the raw state of the external trigger input lines.")]
    public partial class ExternalTriggerState
    {
        /// <summary>
        /// Represents the address of the <see cref="ExternalTriggerState"/> register. This field is constant.
        /// </summary>
        public const int Address = 35;

        /// <summary>
        /// Represents the payload type of the <see cref="ExternalTriggerState"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ExternalTriggerState"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ExternalTriggerState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalInputs GetPayload(HarpMessage message)
        {
            return (DigitalInputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ExternalTriggerState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalInputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ExternalTriggerState"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ExternalTriggerState"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ExternalTriggerState"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ExternalTriggerState"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ExternalTriggerState register.
    /// </summary>
    /// <seealso cref="ExternalTriggerState"/>
    [Description("Filters and selects timestamped messages from the ExternalTriggerState register.")]
    public partial class TimestampedExternalTriggerState
    {
        /// <summary>
        /// Represents the address of the <see cref="ExternalTriggerState"/> register. This field is constant.
        /// </summary>
        public const int Address = ExternalTriggerState.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ExternalTriggerState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetPayload(HarpMessage message)
        {
            return ExternalTriggerState.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.
    /// </summary>
    [Description("Reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.")]
    public partial class AOPortState
    {
        /// <summary>
        /// Represents the address of the <see cref="AOPortState"/> register. This field is constant.
        /// </summary>
        public const int Address = 36;

        /// <summary>
        /// Represents the payload type of the <see cref="AOPortState"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AOPortState"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 4;

        static AOPortStatePayload ParsePayload(float[] payload)
        {
            AOPortStatePayload result;
            result.AOChannel0 = payload[0];
            result.AOChannel1 = payload[1];
            result.AOChannel2 = payload[2];
            result.AOChannel3 = payload[3];
            return result;
        }

        static float[] FormatPayload(AOPortStatePayload value)
        {
            float[] result;
            result = new float[4];
            result[0] = value.AOChannel0;
            result[1] = value.AOChannel1;
            result[2] = value.AOChannel2;
            result[3] = value.AOChannel3;
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="AOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AOPortStatePayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<float>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AOPortStatePayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<float>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AOPortState"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOPortState"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AOPortStatePayload value)
        {
            return HarpMessage.FromSingle(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AOPortState"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOPortState"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AOPortStatePayload value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AOPortState register.
    /// </summary>
    /// <seealso cref="AOPortState"/>
    [Description("Filters and selects timestamped messages from the AOPortState register.")]
    public partial class TimestampedAOPortState
    {
        /// <summary>
        /// Represents the address of the <see cref="AOPortState"/> register. This field is constant.
        /// </summary>
        public const int Address = AOPortState.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AOPortState"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AOPortStatePayload> GetPayload(HarpMessage message)
        {
            return AOPortState.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [Description("Reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class AOChannel0
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel0"/> register. This field is constant.
        /// </summary>
        public const int Address = 37;

        /// <summary>
        /// Represents the payload type of the <see cref="AOChannel0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AOChannel0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="AOChannel0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static float GetPayload(HarpMessage message)
        {
            return message.GetPayloadSingle();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AOChannel0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadSingle();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AOChannel0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AOChannel0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AOChannel0 register.
    /// </summary>
    /// <seealso cref="AOChannel0"/>
    [Description("Filters and selects timestamped messages from the AOChannel0 register.")]
    public partial class TimestampedAOChannel0
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel0"/> register. This field is constant.
        /// </summary>
        public const int Address = AOChannel0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AOChannel0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetPayload(HarpMessage message)
        {
            return AOChannel0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [Description("Reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class AOChannel1
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel1"/> register. This field is constant.
        /// </summary>
        public const int Address = 38;

        /// <summary>
        /// Represents the payload type of the <see cref="AOChannel1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AOChannel1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="AOChannel1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static float GetPayload(HarpMessage message)
        {
            return message.GetPayloadSingle();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AOChannel1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadSingle();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AOChannel1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AOChannel1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AOChannel1 register.
    /// </summary>
    /// <seealso cref="AOChannel1"/>
    [Description("Filters and selects timestamped messages from the AOChannel1 register.")]
    public partial class TimestampedAOChannel1
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel1"/> register. This field is constant.
        /// </summary>
        public const int Address = AOChannel1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AOChannel1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetPayload(HarpMessage message)
        {
            return AOChannel1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [Description("Reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class AOChannel2
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel2"/> register. This field is constant.
        /// </summary>
        public const int Address = 39;

        /// <summary>
        /// Represents the payload type of the <see cref="AOChannel2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AOChannel2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="AOChannel2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static float GetPayload(HarpMessage message)
        {
            return message.GetPayloadSingle();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AOChannel2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadSingle();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AOChannel2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AOChannel2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AOChannel2 register.
    /// </summary>
    /// <seealso cref="AOChannel2"/>
    [Description("Filters and selects timestamped messages from the AOChannel2 register.")]
    public partial class TimestampedAOChannel2
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel2"/> register. This field is constant.
        /// </summary>
        public const int Address = AOChannel2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AOChannel2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetPayload(HarpMessage message)
        {
            return AOChannel2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [Description("Reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class AOChannel3
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel3"/> register. This field is constant.
        /// </summary>
        public const int Address = 40;

        /// <summary>
        /// Represents the payload type of the <see cref="AOChannel3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AOChannel3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="AOChannel3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static float GetPayload(HarpMessage message)
        {
            return message.GetPayloadSingle();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AOChannel3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadSingle();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AOChannel3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AOChannel3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AOChannel3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, float value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AOChannel3 register.
    /// </summary>
    /// <seealso cref="AOChannel3"/>
    [Description("Filters and selects timestamped messages from the AOChannel3 register.")]
    public partial class TimestampedAOChannel3
    {
        /// <summary>
        /// Represents the address of the <see cref="AOChannel3"/> register. This field is constant.
        /// </summary>
        public const int Address = AOChannel3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AOChannel3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<float> GetPayload(HarpMessage message)
        {
            return AOChannel3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reflects which analog output channels are configured and ready to start.
    /// </summary>
    [Description("Reflects which analog output channels are configured and ready to start.")]
    public partial class DacReady
    {
        /// <summary>
        /// Represents the address of the <see cref="DacReady"/> register. This field is constant.
        /// </summary>
        public const int Address = 41;

        /// <summary>
        /// Represents the payload type of the <see cref="DacReady"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DacReady"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DacReady"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogOutputs GetPayload(HarpMessage message)
        {
            return (AnalogOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DacReady"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((AnalogOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DacReady"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacReady"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DacReady"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacReady"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DacReady register.
    /// </summary>
    /// <seealso cref="DacReady"/>
    [Description("Filters and selects timestamped messages from the DacReady register.")]
    public partial class TimestampedDacReady
    {
        /// <summary>
        /// Represents the address of the <see cref="DacReady"/> register. This field is constant.
        /// </summary>
        public const int Address = DacReady.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DacReady"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetPayload(HarpMessage message)
        {
            return DacReady.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
    /// </summary>
    [Description("Starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.")]
    public partial class DacStart
    {
        /// <summary>
        /// Represents the address of the <see cref="DacStart"/> register. This field is constant.
        /// </summary>
        public const int Address = 42;

        /// <summary>
        /// Represents the payload type of the <see cref="DacStart"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DacStart"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DacStart"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogOutputs GetPayload(HarpMessage message)
        {
            return (AnalogOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DacStart"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((AnalogOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DacStart"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacStart"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DacStart"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacStart"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DacStart register.
    /// </summary>
    /// <seealso cref="DacStart"/>
    [Description("Filters and selects timestamped messages from the DacStart register.")]
    public partial class TimestampedDacStart
    {
        /// <summary>
        /// Represents the address of the <see cref="DacStart"/> register. This field is constant.
        /// </summary>
        public const int Address = DacStart.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DacStart"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetPayload(HarpMessage message)
        {
            return DacStart.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that pauses the channels set in the mask and resumes those cleared in it.
    /// </summary>
    [Description("Pauses the channels set in the mask and resumes those cleared in it.")]
    public partial class DacPause
    {
        /// <summary>
        /// Represents the address of the <see cref="DacPause"/> register. This field is constant.
        /// </summary>
        public const int Address = 43;

        /// <summary>
        /// Represents the payload type of the <see cref="DacPause"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DacPause"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DacPause"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogOutputs GetPayload(HarpMessage message)
        {
            return (AnalogOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DacPause"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((AnalogOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DacPause"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacPause"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DacPause"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacPause"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DacPause register.
    /// </summary>
    /// <seealso cref="DacPause"/>
    [Description("Filters and selects timestamped messages from the DacPause register.")]
    public partial class TimestampedDacPause
    {
        /// <summary>
        /// Represents the address of the <see cref="DacPause"/> register. This field is constant.
        /// </summary>
        public const int Address = DacPause.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DacPause"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetPayload(HarpMessage message)
        {
            return DacPause.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that aborts waveform playback on the analog output channels specified in the mask.
    /// </summary>
    [Description("Aborts waveform playback on the analog output channels specified in the mask.")]
    public partial class DacAbort
    {
        /// <summary>
        /// Represents the address of the <see cref="DacAbort"/> register. This field is constant.
        /// </summary>
        public const int Address = 44;

        /// <summary>
        /// Represents the payload type of the <see cref="DacAbort"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DacAbort"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DacAbort"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogOutputs GetPayload(HarpMessage message)
        {
            return (AnalogOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DacAbort"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((AnalogOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DacAbort"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacAbort"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DacAbort"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacAbort"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DacAbort register.
    /// </summary>
    /// <seealso cref="DacAbort"/>
    [Description("Filters and selects timestamped messages from the DacAbort register.")]
    public partial class TimestampedDacAbort
    {
        /// <summary>
        /// Represents the address of the <see cref="DacAbort"/> register. This field is constant.
        /// </summary>
        public const int Address = DacAbort.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DacAbort"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetPayload(HarpMessage message)
        {
            return DacAbort.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that reports which analog output channels have finished playing their waveform.
    /// </summary>
    [Description("Reports which analog output channels have finished playing their waveform.")]
    public partial class DacFinished
    {
        /// <summary>
        /// Represents the address of the <see cref="DacFinished"/> register. This field is constant.
        /// </summary>
        public const int Address = 45;

        /// <summary>
        /// Represents the payload type of the <see cref="DacFinished"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DacFinished"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DacFinished"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogOutputs GetPayload(HarpMessage message)
        {
            return (AnalogOutputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DacFinished"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((AnalogOutputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DacFinished"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacFinished"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DacFinished"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DacFinished"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogOutputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DacFinished register.
    /// </summary>
    /// <seealso cref="DacFinished"/>
    [Description("Filters and selects timestamped messages from the DacFinished register.")]
    public partial class TimestampedDacFinished
    {
        /// <summary>
        /// Represents the address of the <see cref="DacFinished"/> register. This field is constant.
        /// </summary>
        public const int Address = DacFinished.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DacFinished"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogOutputs> GetPayload(HarpMessage message)
        {
            return DacFinished.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which external trigger lines can start channel 0.
    /// </summary>
    [Description("Specifies which external trigger lines can start channel 0.")]
    public partial class ChannelExternalTriggers0
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers0"/> register. This field is constant.
        /// </summary>
        public const int Address = 46;

        /// <summary>
        /// Represents the payload type of the <see cref="ChannelExternalTriggers0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ChannelExternalTriggers0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ChannelExternalTriggers0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalInputs GetPayload(HarpMessage message)
        {
            return (DigitalInputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ChannelExternalTriggers0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalInputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ChannelExternalTriggers0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ChannelExternalTriggers0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ChannelExternalTriggers0 register.
    /// </summary>
    /// <seealso cref="ChannelExternalTriggers0"/>
    [Description("Filters and selects timestamped messages from the ChannelExternalTriggers0 register.")]
    public partial class TimestampedChannelExternalTriggers0
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers0"/> register. This field is constant.
        /// </summary>
        public const int Address = ChannelExternalTriggers0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ChannelExternalTriggers0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetPayload(HarpMessage message)
        {
            return ChannelExternalTriggers0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which external trigger lines can start channel 1.
    /// </summary>
    [Description("Specifies which external trigger lines can start channel 1.")]
    public partial class ChannelExternalTriggers1
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers1"/> register. This field is constant.
        /// </summary>
        public const int Address = 47;

        /// <summary>
        /// Represents the payload type of the <see cref="ChannelExternalTriggers1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ChannelExternalTriggers1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ChannelExternalTriggers1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalInputs GetPayload(HarpMessage message)
        {
            return (DigitalInputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ChannelExternalTriggers1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalInputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ChannelExternalTriggers1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ChannelExternalTriggers1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ChannelExternalTriggers1 register.
    /// </summary>
    /// <seealso cref="ChannelExternalTriggers1"/>
    [Description("Filters and selects timestamped messages from the ChannelExternalTriggers1 register.")]
    public partial class TimestampedChannelExternalTriggers1
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers1"/> register. This field is constant.
        /// </summary>
        public const int Address = ChannelExternalTriggers1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ChannelExternalTriggers1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetPayload(HarpMessage message)
        {
            return ChannelExternalTriggers1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which external trigger lines can start channel 2.
    /// </summary>
    [Description("Specifies which external trigger lines can start channel 2.")]
    public partial class ChannelExternalTriggers2
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers2"/> register. This field is constant.
        /// </summary>
        public const int Address = 48;

        /// <summary>
        /// Represents the payload type of the <see cref="ChannelExternalTriggers2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ChannelExternalTriggers2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ChannelExternalTriggers2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalInputs GetPayload(HarpMessage message)
        {
            return (DigitalInputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ChannelExternalTriggers2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalInputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ChannelExternalTriggers2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ChannelExternalTriggers2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ChannelExternalTriggers2 register.
    /// </summary>
    /// <seealso cref="ChannelExternalTriggers2"/>
    [Description("Filters and selects timestamped messages from the ChannelExternalTriggers2 register.")]
    public partial class TimestampedChannelExternalTriggers2
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers2"/> register. This field is constant.
        /// </summary>
        public const int Address = ChannelExternalTriggers2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ChannelExternalTriggers2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetPayload(HarpMessage message)
        {
            return ChannelExternalTriggers2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which external trigger lines can start channel 3.
    /// </summary>
    [Description("Specifies which external trigger lines can start channel 3.")]
    public partial class ChannelExternalTriggers3
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers3"/> register. This field is constant.
        /// </summary>
        public const int Address = 49;

        /// <summary>
        /// Represents the payload type of the <see cref="ChannelExternalTriggers3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ChannelExternalTriggers3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ChannelExternalTriggers3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static DigitalInputs GetPayload(HarpMessage message)
        {
            return (DigitalInputs)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ChannelExternalTriggers3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((DigitalInputs)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ChannelExternalTriggers3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ChannelExternalTriggers3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ChannelExternalTriggers3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, DigitalInputs value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ChannelExternalTriggers3 register.
    /// </summary>
    /// <seealso cref="ChannelExternalTriggers3"/>
    [Description("Filters and selects timestamped messages from the ChannelExternalTriggers3 register.")]
    public partial class TimestampedChannelExternalTriggers3
    {
        /// <summary>
        /// Represents the address of the <see cref="ChannelExternalTriggers3"/> register. This field is constant.
        /// </summary>
        public const int Address = ChannelExternalTriggers3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ChannelExternalTriggers3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<DigitalInputs> GetPayload(HarpMessage message)
        {
            return ChannelExternalTriggers3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which waveform player drives analog output channel 0.
    /// </summary>
    [Description("Specifies which waveform player drives analog output channel 0.")]
    public partial class ActivePlayer0
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer0"/> register. This field is constant.
        /// </summary>
        public const int Address = 50;

        /// <summary>
        /// Represents the payload type of the <see cref="ActivePlayer0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ActivePlayer0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ActivePlayer0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static PlayerType GetPayload(HarpMessage message)
        {
            return (PlayerType)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ActivePlayer0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((PlayerType)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ActivePlayer0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ActivePlayer0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ActivePlayer0 register.
    /// </summary>
    /// <seealso cref="ActivePlayer0"/>
    [Description("Filters and selects timestamped messages from the ActivePlayer0 register.")]
    public partial class TimestampedActivePlayer0
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer0"/> register. This field is constant.
        /// </summary>
        public const int Address = ActivePlayer0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ActivePlayer0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetPayload(HarpMessage message)
        {
            return ActivePlayer0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which waveform player drives analog output channel 1.
    /// </summary>
    [Description("Specifies which waveform player drives analog output channel 1.")]
    public partial class ActivePlayer1
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer1"/> register. This field is constant.
        /// </summary>
        public const int Address = 51;

        /// <summary>
        /// Represents the payload type of the <see cref="ActivePlayer1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ActivePlayer1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ActivePlayer1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static PlayerType GetPayload(HarpMessage message)
        {
            return (PlayerType)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ActivePlayer1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((PlayerType)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ActivePlayer1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ActivePlayer1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ActivePlayer1 register.
    /// </summary>
    /// <seealso cref="ActivePlayer1"/>
    [Description("Filters and selects timestamped messages from the ActivePlayer1 register.")]
    public partial class TimestampedActivePlayer1
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer1"/> register. This field is constant.
        /// </summary>
        public const int Address = ActivePlayer1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ActivePlayer1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetPayload(HarpMessage message)
        {
            return ActivePlayer1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which waveform player drives analog output channel 2.
    /// </summary>
    [Description("Specifies which waveform player drives analog output channel 2.")]
    public partial class ActivePlayer2
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer2"/> register. This field is constant.
        /// </summary>
        public const int Address = 52;

        /// <summary>
        /// Represents the payload type of the <see cref="ActivePlayer2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ActivePlayer2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ActivePlayer2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static PlayerType GetPayload(HarpMessage message)
        {
            return (PlayerType)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ActivePlayer2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((PlayerType)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ActivePlayer2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ActivePlayer2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ActivePlayer2 register.
    /// </summary>
    /// <seealso cref="ActivePlayer2"/>
    [Description("Filters and selects timestamped messages from the ActivePlayer2 register.")]
    public partial class TimestampedActivePlayer2
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer2"/> register. This field is constant.
        /// </summary>
        public const int Address = ActivePlayer2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ActivePlayer2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetPayload(HarpMessage message)
        {
            return ActivePlayer2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies which waveform player drives analog output channel 3.
    /// </summary>
    [Description("Specifies which waveform player drives analog output channel 3.")]
    public partial class ActivePlayer3
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer3"/> register. This field is constant.
        /// </summary>
        public const int Address = 53;

        /// <summary>
        /// Represents the payload type of the <see cref="ActivePlayer3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ActivePlayer3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="ActivePlayer3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static PlayerType GetPayload(HarpMessage message)
        {
            return (PlayerType)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ActivePlayer3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((PlayerType)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ActivePlayer3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ActivePlayer3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ActivePlayer3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, PlayerType value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ActivePlayer3 register.
    /// </summary>
    /// <seealso cref="ActivePlayer3"/>
    [Description("Filters and selects timestamped messages from the ActivePlayer3 register.")]
    public partial class TimestampedActivePlayer3
    {
        /// <summary>
        /// Represents the address of the <see cref="ActivePlayer3"/> register. This field is constant.
        /// </summary>
        public const int Address = ActivePlayer3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ActivePlayer3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PlayerType> GetPayload(HarpMessage message)
        {
            return ActivePlayer3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the file player settings for analog output channel 0.
    /// </summary>
    [Description("Specifies the file player settings for analog output channel 0.")]
    public partial class FileSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = 54;

        /// <summary>
        /// Represents the payload type of the <see cref="FileSettings0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="FileSettings0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 45;

        static FileSettings0Payload ParsePayload(byte[] payload)
        {
            FileSettings0Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Path = PayloadMarshal.ReadUtf8String(new ArraySegment<byte>(payload, 12, 33));
            return result;
        }

        static byte[] FormatPayload(FileSettings0Payload value)
        {
            byte[] result;
            result = new byte[45];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 33), value.Path);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="FileSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static FileSettings0Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="FileSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings0Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="FileSettings0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, FileSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="FileSettings0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, FileSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// FileSettings0 register.
    /// </summary>
    /// <seealso cref="FileSettings0"/>
    [Description("Filters and selects timestamped messages from the FileSettings0 register.")]
    public partial class TimestampedFileSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = FileSettings0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="FileSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings0Payload> GetPayload(HarpMessage message)
        {
            return FileSettings0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the file player settings for analog output channel 1.
    /// </summary>
    [Description("Specifies the file player settings for analog output channel 1.")]
    public partial class FileSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = 55;

        /// <summary>
        /// Represents the payload type of the <see cref="FileSettings1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="FileSettings1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 45;

        static FileSettings1Payload ParsePayload(byte[] payload)
        {
            FileSettings1Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Path = PayloadMarshal.ReadUtf8String(new ArraySegment<byte>(payload, 12, 33));
            return result;
        }

        static byte[] FormatPayload(FileSettings1Payload value)
        {
            byte[] result;
            result = new byte[45];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 33), value.Path);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="FileSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static FileSettings1Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="FileSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings1Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="FileSettings1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, FileSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="FileSettings1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, FileSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// FileSettings1 register.
    /// </summary>
    /// <seealso cref="FileSettings1"/>
    [Description("Filters and selects timestamped messages from the FileSettings1 register.")]
    public partial class TimestampedFileSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = FileSettings1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="FileSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings1Payload> GetPayload(HarpMessage message)
        {
            return FileSettings1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the file player settings for analog output channel 2.
    /// </summary>
    [Description("Specifies the file player settings for analog output channel 2.")]
    public partial class FileSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = 56;

        /// <summary>
        /// Represents the payload type of the <see cref="FileSettings2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="FileSettings2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 45;

        static FileSettings2Payload ParsePayload(byte[] payload)
        {
            FileSettings2Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Path = PayloadMarshal.ReadUtf8String(new ArraySegment<byte>(payload, 12, 33));
            return result;
        }

        static byte[] FormatPayload(FileSettings2Payload value)
        {
            byte[] result;
            result = new byte[45];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 33), value.Path);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="FileSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static FileSettings2Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="FileSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings2Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="FileSettings2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, FileSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="FileSettings2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, FileSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// FileSettings2 register.
    /// </summary>
    /// <seealso cref="FileSettings2"/>
    [Description("Filters and selects timestamped messages from the FileSettings2 register.")]
    public partial class TimestampedFileSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = FileSettings2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="FileSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings2Payload> GetPayload(HarpMessage message)
        {
            return FileSettings2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the file player settings for analog output channel 3.
    /// </summary>
    [Description("Specifies the file player settings for analog output channel 3.")]
    public partial class FileSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = 57;

        /// <summary>
        /// Represents the payload type of the <see cref="FileSettings3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="FileSettings3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 45;

        static FileSettings3Payload ParsePayload(byte[] payload)
        {
            FileSettings3Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Path = PayloadMarshal.ReadUtf8String(new ArraySegment<byte>(payload, 12, 33));
            return result;
        }

        static byte[] FormatPayload(FileSettings3Payload value)
        {
            byte[] result;
            result = new byte[45];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 33), value.Path);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="FileSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static FileSettings3Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="FileSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings3Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="FileSettings3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, FileSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="FileSettings3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="FileSettings3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, FileSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// FileSettings3 register.
    /// </summary>
    /// <seealso cref="FileSettings3"/>
    [Description("Filters and selects timestamped messages from the FileSettings3 register.")]
    public partial class TimestampedFileSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="FileSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = FileSettings3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="FileSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<FileSettings3Payload> GetPayload(HarpMessage message)
        {
            return FileSettings3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the sine player settings for analog output channel 0.
    /// </summary>
    [Description("Specifies the sine player settings for analog output channel 0.")]
    public partial class SineSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = 58;

        /// <summary>
        /// Represents the payload type of the <see cref="SineSettings0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="SineSettings0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 28;

        static SineSettings0Payload ParsePayload(byte[] payload)
        {
            SineSettings0Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            return result;
        }

        static byte[] FormatPayload(SineSettings0Payload value)
        {
            byte[] result;
            result = new byte[28];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="SineSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static SineSettings0Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="SineSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings0Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="SineSettings0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, SineSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="SineSettings0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, SineSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// SineSettings0 register.
    /// </summary>
    /// <seealso cref="SineSettings0"/>
    [Description("Filters and selects timestamped messages from the SineSettings0 register.")]
    public partial class TimestampedSineSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = SineSettings0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="SineSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings0Payload> GetPayload(HarpMessage message)
        {
            return SineSettings0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the sine player settings for analog output channel 1.
    /// </summary>
    [Description("Specifies the sine player settings for analog output channel 1.")]
    public partial class SineSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = 59;

        /// <summary>
        /// Represents the payload type of the <see cref="SineSettings1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="SineSettings1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 28;

        static SineSettings1Payload ParsePayload(byte[] payload)
        {
            SineSettings1Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            return result;
        }

        static byte[] FormatPayload(SineSettings1Payload value)
        {
            byte[] result;
            result = new byte[28];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="SineSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static SineSettings1Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="SineSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings1Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="SineSettings1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, SineSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="SineSettings1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, SineSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// SineSettings1 register.
    /// </summary>
    /// <seealso cref="SineSettings1"/>
    [Description("Filters and selects timestamped messages from the SineSettings1 register.")]
    public partial class TimestampedSineSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = SineSettings1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="SineSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings1Payload> GetPayload(HarpMessage message)
        {
            return SineSettings1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the sine player settings for analog output channel 2.
    /// </summary>
    [Description("Specifies the sine player settings for analog output channel 2.")]
    public partial class SineSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = 60;

        /// <summary>
        /// Represents the payload type of the <see cref="SineSettings2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="SineSettings2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 28;

        static SineSettings2Payload ParsePayload(byte[] payload)
        {
            SineSettings2Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            return result;
        }

        static byte[] FormatPayload(SineSettings2Payload value)
        {
            byte[] result;
            result = new byte[28];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="SineSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static SineSettings2Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="SineSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings2Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="SineSettings2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, SineSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="SineSettings2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, SineSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// SineSettings2 register.
    /// </summary>
    /// <seealso cref="SineSettings2"/>
    [Description("Filters and selects timestamped messages from the SineSettings2 register.")]
    public partial class TimestampedSineSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = SineSettings2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="SineSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings2Payload> GetPayload(HarpMessage message)
        {
            return SineSettings2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the sine player settings for analog output channel 3.
    /// </summary>
    [Description("Specifies the sine player settings for analog output channel 3.")]
    public partial class SineSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = 61;

        /// <summary>
        /// Represents the payload type of the <see cref="SineSettings3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="SineSettings3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 28;

        static SineSettings3Payload ParsePayload(byte[] payload)
        {
            SineSettings3Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            return result;
        }

        static byte[] FormatPayload(SineSettings3Payload value)
        {
            byte[] result;
            result = new byte[28];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="SineSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static SineSettings3Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="SineSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings3Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="SineSettings3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, SineSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="SineSettings3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="SineSettings3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, SineSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// SineSettings3 register.
    /// </summary>
    /// <seealso cref="SineSettings3"/>
    [Description("Filters and selects timestamped messages from the SineSettings3 register.")]
    public partial class TimestampedSineSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="SineSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = SineSettings3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="SineSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<SineSettings3Payload> GetPayload(HarpMessage message)
        {
            return SineSettings3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the trapezoid player settings for analog output channel 0.
    /// </summary>
    [Description("Specifies the trapezoid player settings for analog output channel 0.")]
    public partial class TrapezoidSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = 62;

        /// <summary>
        /// Represents the payload type of the <see cref="TrapezoidSettings0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="TrapezoidSettings0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 40;

        static TrapezoidSettings0Payload ParsePayload(byte[] payload)
        {
            TrapezoidSettings0Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            result.RampOnDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 28, 4));
            result.PulseWidthDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 32, 4));
            result.RampOffDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 36, 4));
            return result;
        }

        static byte[] FormatPayload(TrapezoidSettings0Payload value)
        {
            byte[] result;
            result = new byte[40];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 28, 4), value.RampOnDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 32, 4), value.PulseWidthDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 36, 4), value.RampOffDuration);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="TrapezoidSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static TrapezoidSettings0Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="TrapezoidSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings0Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="TrapezoidSettings0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, TrapezoidSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="TrapezoidSettings0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, TrapezoidSettings0Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// TrapezoidSettings0 register.
    /// </summary>
    /// <seealso cref="TrapezoidSettings0"/>
    [Description("Filters and selects timestamped messages from the TrapezoidSettings0 register.")]
    public partial class TimestampedTrapezoidSettings0
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings0"/> register. This field is constant.
        /// </summary>
        public const int Address = TrapezoidSettings0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="TrapezoidSettings0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings0Payload> GetPayload(HarpMessage message)
        {
            return TrapezoidSettings0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the trapezoid player settings for analog output channel 1.
    /// </summary>
    [Description("Specifies the trapezoid player settings for analog output channel 1.")]
    public partial class TrapezoidSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = 63;

        /// <summary>
        /// Represents the payload type of the <see cref="TrapezoidSettings1"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="TrapezoidSettings1"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 40;

        static TrapezoidSettings1Payload ParsePayload(byte[] payload)
        {
            TrapezoidSettings1Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            result.RampOnDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 28, 4));
            result.PulseWidthDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 32, 4));
            result.RampOffDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 36, 4));
            return result;
        }

        static byte[] FormatPayload(TrapezoidSettings1Payload value)
        {
            byte[] result;
            result = new byte[40];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 28, 4), value.RampOnDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 32, 4), value.PulseWidthDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 36, 4), value.RampOffDuration);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="TrapezoidSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static TrapezoidSettings1Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="TrapezoidSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings1Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="TrapezoidSettings1"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings1"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, TrapezoidSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="TrapezoidSettings1"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings1"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, TrapezoidSettings1Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// TrapezoidSettings1 register.
    /// </summary>
    /// <seealso cref="TrapezoidSettings1"/>
    [Description("Filters and selects timestamped messages from the TrapezoidSettings1 register.")]
    public partial class TimestampedTrapezoidSettings1
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings1"/> register. This field is constant.
        /// </summary>
        public const int Address = TrapezoidSettings1.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="TrapezoidSettings1"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings1Payload> GetPayload(HarpMessage message)
        {
            return TrapezoidSettings1.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the trapezoid player settings for analog output channel 2.
    /// </summary>
    [Description("Specifies the trapezoid player settings for analog output channel 2.")]
    public partial class TrapezoidSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = 64;

        /// <summary>
        /// Represents the payload type of the <see cref="TrapezoidSettings2"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="TrapezoidSettings2"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 40;

        static TrapezoidSettings2Payload ParsePayload(byte[] payload)
        {
            TrapezoidSettings2Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            result.RampOnDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 28, 4));
            result.PulseWidthDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 32, 4));
            result.RampOffDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 36, 4));
            return result;
        }

        static byte[] FormatPayload(TrapezoidSettings2Payload value)
        {
            byte[] result;
            result = new byte[40];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 28, 4), value.RampOnDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 32, 4), value.PulseWidthDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 36, 4), value.RampOffDuration);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="TrapezoidSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static TrapezoidSettings2Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="TrapezoidSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings2Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="TrapezoidSettings2"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings2"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, TrapezoidSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="TrapezoidSettings2"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings2"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, TrapezoidSettings2Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// TrapezoidSettings2 register.
    /// </summary>
    /// <seealso cref="TrapezoidSettings2"/>
    [Description("Filters and selects timestamped messages from the TrapezoidSettings2 register.")]
    public partial class TimestampedTrapezoidSettings2
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings2"/> register. This field is constant.
        /// </summary>
        public const int Address = TrapezoidSettings2.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="TrapezoidSettings2"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings2Payload> GetPayload(HarpMessage message)
        {
            return TrapezoidSettings2.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that specifies the trapezoid player settings for analog output channel 3.
    /// </summary>
    [Description("Specifies the trapezoid player settings for analog output channel 3.")]
    public partial class TrapezoidSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = 65;

        /// <summary>
        /// Represents the payload type of the <see cref="TrapezoidSettings3"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="TrapezoidSettings3"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 40;

        static TrapezoidSettings3Payload ParsePayload(byte[] payload)
        {
            TrapezoidSettings3Payload result;
            result.Cycles = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 0, 4));
            result.Duration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 4, 4));
            result.UpdateFrequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 8, 4));
            result.Frequency = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 12, 4));
            result.Amplitude = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 16, 4));
            result.VerticalShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 20, 4));
            result.NormalizedPhaseShift = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 24, 4));
            result.RampOnDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 28, 4));
            result.PulseWidthDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 32, 4));
            result.RampOffDuration = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 36, 4));
            return result;
        }

        static byte[] FormatPayload(TrapezoidSettings3Payload value)
        {
            byte[] result;
            result = new byte[40];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 4), value.Cycles);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.Duration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.UpdateFrequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 4), value.Frequency);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 16, 4), value.Amplitude);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 20, 4), value.VerticalShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 24, 4), value.NormalizedPhaseShift);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 28, 4), value.RampOnDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 32, 4), value.PulseWidthDuration);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 36, 4), value.RampOffDuration);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="TrapezoidSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static TrapezoidSettings3Payload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="TrapezoidSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings3Payload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="TrapezoidSettings3"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings3"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, TrapezoidSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="TrapezoidSettings3"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="TrapezoidSettings3"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, TrapezoidSettings3Payload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// TrapezoidSettings3 register.
    /// </summary>
    /// <seealso cref="TrapezoidSettings3"/>
    [Description("Filters and selects timestamped messages from the TrapezoidSettings3 register.")]
    public partial class TimestampedTrapezoidSettings3
    {
        /// <summary>
        /// Represents the address of the <see cref="TrapezoidSettings3"/> register. This field is constant.
        /// </summary>
        public const int Address = TrapezoidSettings3.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="TrapezoidSettings3"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<TrapezoidSettings3Payload> GetPayload(HarpMessage message)
        {
            return TrapezoidSettings3.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents an operator which creates standard message payloads for the
    /// Quac device.
    /// </summary>
    /// <seealso cref="CreateDOPortStatePayload"/>
    /// <seealso cref="CreateDOPortSetPayload"/>
    /// <seealso cref="CreateDOPortClearPayload"/>
    /// <seealso cref="CreateExternalTriggerStatePayload"/>
    /// <seealso cref="CreateAOPortStatePayload"/>
    /// <seealso cref="CreateAOChannel0Payload"/>
    /// <seealso cref="CreateAOChannel1Payload"/>
    /// <seealso cref="CreateAOChannel2Payload"/>
    /// <seealso cref="CreateAOChannel3Payload"/>
    /// <seealso cref="CreateDacReadyPayload"/>
    /// <seealso cref="CreateDacStartPayload"/>
    /// <seealso cref="CreateDacPausePayload"/>
    /// <seealso cref="CreateDacAbortPayload"/>
    /// <seealso cref="CreateDacFinishedPayload"/>
    /// <seealso cref="CreateChannelExternalTriggers0Payload"/>
    /// <seealso cref="CreateChannelExternalTriggers1Payload"/>
    /// <seealso cref="CreateChannelExternalTriggers2Payload"/>
    /// <seealso cref="CreateChannelExternalTriggers3Payload"/>
    /// <seealso cref="CreateActivePlayer0Payload"/>
    /// <seealso cref="CreateActivePlayer1Payload"/>
    /// <seealso cref="CreateActivePlayer2Payload"/>
    /// <seealso cref="CreateActivePlayer3Payload"/>
    /// <seealso cref="CreateFileSettings0Payload"/>
    /// <seealso cref="CreateFileSettings1Payload"/>
    /// <seealso cref="CreateFileSettings2Payload"/>
    /// <seealso cref="CreateFileSettings3Payload"/>
    /// <seealso cref="CreateSineSettings0Payload"/>
    /// <seealso cref="CreateSineSettings1Payload"/>
    /// <seealso cref="CreateSineSettings2Payload"/>
    /// <seealso cref="CreateSineSettings3Payload"/>
    /// <seealso cref="CreateTrapezoidSettings0Payload"/>
    /// <seealso cref="CreateTrapezoidSettings1Payload"/>
    /// <seealso cref="CreateTrapezoidSettings2Payload"/>
    /// <seealso cref="CreateTrapezoidSettings3Payload"/>
    [XmlInclude(typeof(CreateDOPortStatePayload))]
    [XmlInclude(typeof(CreateDOPortSetPayload))]
    [XmlInclude(typeof(CreateDOPortClearPayload))]
    [XmlInclude(typeof(CreateExternalTriggerStatePayload))]
    [XmlInclude(typeof(CreateAOPortStatePayload))]
    [XmlInclude(typeof(CreateAOChannel0Payload))]
    [XmlInclude(typeof(CreateAOChannel1Payload))]
    [XmlInclude(typeof(CreateAOChannel2Payload))]
    [XmlInclude(typeof(CreateAOChannel3Payload))]
    [XmlInclude(typeof(CreateDacReadyPayload))]
    [XmlInclude(typeof(CreateDacStartPayload))]
    [XmlInclude(typeof(CreateDacPausePayload))]
    [XmlInclude(typeof(CreateDacAbortPayload))]
    [XmlInclude(typeof(CreateDacFinishedPayload))]
    [XmlInclude(typeof(CreateChannelExternalTriggers0Payload))]
    [XmlInclude(typeof(CreateChannelExternalTriggers1Payload))]
    [XmlInclude(typeof(CreateChannelExternalTriggers2Payload))]
    [XmlInclude(typeof(CreateChannelExternalTriggers3Payload))]
    [XmlInclude(typeof(CreateActivePlayer0Payload))]
    [XmlInclude(typeof(CreateActivePlayer1Payload))]
    [XmlInclude(typeof(CreateActivePlayer2Payload))]
    [XmlInclude(typeof(CreateActivePlayer3Payload))]
    [XmlInclude(typeof(CreateFileSettings0Payload))]
    [XmlInclude(typeof(CreateFileSettings1Payload))]
    [XmlInclude(typeof(CreateFileSettings2Payload))]
    [XmlInclude(typeof(CreateFileSettings3Payload))]
    [XmlInclude(typeof(CreateSineSettings0Payload))]
    [XmlInclude(typeof(CreateSineSettings1Payload))]
    [XmlInclude(typeof(CreateSineSettings2Payload))]
    [XmlInclude(typeof(CreateSineSettings3Payload))]
    [XmlInclude(typeof(CreateTrapezoidSettings0Payload))]
    [XmlInclude(typeof(CreateTrapezoidSettings1Payload))]
    [XmlInclude(typeof(CreateTrapezoidSettings2Payload))]
    [XmlInclude(typeof(CreateTrapezoidSettings3Payload))]
    [XmlInclude(typeof(CreateTimestampedDOPortStatePayload))]
    [XmlInclude(typeof(CreateTimestampedDOPortSetPayload))]
    [XmlInclude(typeof(CreateTimestampedDOPortClearPayload))]
    [XmlInclude(typeof(CreateTimestampedExternalTriggerStatePayload))]
    [XmlInclude(typeof(CreateTimestampedAOPortStatePayload))]
    [XmlInclude(typeof(CreateTimestampedAOChannel0Payload))]
    [XmlInclude(typeof(CreateTimestampedAOChannel1Payload))]
    [XmlInclude(typeof(CreateTimestampedAOChannel2Payload))]
    [XmlInclude(typeof(CreateTimestampedAOChannel3Payload))]
    [XmlInclude(typeof(CreateTimestampedDacReadyPayload))]
    [XmlInclude(typeof(CreateTimestampedDacStartPayload))]
    [XmlInclude(typeof(CreateTimestampedDacPausePayload))]
    [XmlInclude(typeof(CreateTimestampedDacAbortPayload))]
    [XmlInclude(typeof(CreateTimestampedDacFinishedPayload))]
    [XmlInclude(typeof(CreateTimestampedChannelExternalTriggers0Payload))]
    [XmlInclude(typeof(CreateTimestampedChannelExternalTriggers1Payload))]
    [XmlInclude(typeof(CreateTimestampedChannelExternalTriggers2Payload))]
    [XmlInclude(typeof(CreateTimestampedChannelExternalTriggers3Payload))]
    [XmlInclude(typeof(CreateTimestampedActivePlayer0Payload))]
    [XmlInclude(typeof(CreateTimestampedActivePlayer1Payload))]
    [XmlInclude(typeof(CreateTimestampedActivePlayer2Payload))]
    [XmlInclude(typeof(CreateTimestampedActivePlayer3Payload))]
    [XmlInclude(typeof(CreateTimestampedFileSettings0Payload))]
    [XmlInclude(typeof(CreateTimestampedFileSettings1Payload))]
    [XmlInclude(typeof(CreateTimestampedFileSettings2Payload))]
    [XmlInclude(typeof(CreateTimestampedFileSettings3Payload))]
    [XmlInclude(typeof(CreateTimestampedSineSettings0Payload))]
    [XmlInclude(typeof(CreateTimestampedSineSettings1Payload))]
    [XmlInclude(typeof(CreateTimestampedSineSettings2Payload))]
    [XmlInclude(typeof(CreateTimestampedSineSettings3Payload))]
    [XmlInclude(typeof(CreateTimestampedTrapezoidSettings0Payload))]
    [XmlInclude(typeof(CreateTimestampedTrapezoidSettings1Payload))]
    [XmlInclude(typeof(CreateTimestampedTrapezoidSettings2Payload))]
    [XmlInclude(typeof(CreateTimestampedTrapezoidSettings3Payload))]
    [Description("Creates standard message payloads for the Quac device.")]
    public partial class CreateMessage : CreateMessageBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateMessage"/> class.
        /// </summary>
        public CreateMessage()
        {
            Payload = new CreateDOPortStatePayload();
        }

        string INamedElement.Name => $"{nameof(Quac)}.{GetElementDisplayName(Payload)}";
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the state of the digital output lines.
    /// </summary>
    [DisplayName("DOPortStatePayload")]
    [Description("Creates a message payload that reflects and specifies the state of the digital output lines.")]
    public partial class CreateDOPortStatePayload
    {
        /// <summary>
        /// Gets or sets the value that reflects and specifies the state of the digital output lines.
        /// </summary>
        [Description("The value that reflects and specifies the state of the digital output lines.")]
        public DigitalOutputs DOPortState { get; set; }

        /// <summary>
        /// Creates a message payload for the DOPortState register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalOutputs GetPayload()
        {
            return DOPortState;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the state of the digital output lines.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DOPortState register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortState.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the state of the digital output lines.
    /// </summary>
    [DisplayName("TimestampedDOPortStatePayload")]
    [Description("Creates a timestamped message payload that reflects and specifies the state of the digital output lines.")]
    public partial class CreateTimestampedDOPortStatePayload : CreateDOPortStatePayload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the state of the digital output lines.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DOPortState register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortState.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that sets the digital output lines specified in the mask to logic HIGH.
    /// </summary>
    [DisplayName("DOPortSetPayload")]
    [Description("Creates a message payload that sets the digital output lines specified in the mask to logic HIGH.")]
    public partial class CreateDOPortSetPayload
    {
        /// <summary>
        /// Gets or sets the value that sets the digital output lines specified in the mask to logic HIGH.
        /// </summary>
        [Description("The value that sets the digital output lines specified in the mask to logic HIGH.")]
        public DigitalOutputs DOPortSet { get; set; }

        /// <summary>
        /// Creates a message payload for the DOPortSet register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalOutputs GetPayload()
        {
            return DOPortSet;
        }

        /// <summary>
        /// Creates a message that sets the digital output lines specified in the mask to logic HIGH.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DOPortSet register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortSet.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that sets the digital output lines specified in the mask to logic HIGH.
    /// </summary>
    [DisplayName("TimestampedDOPortSetPayload")]
    [Description("Creates a timestamped message payload that sets the digital output lines specified in the mask to logic HIGH.")]
    public partial class CreateTimestampedDOPortSetPayload : CreateDOPortSetPayload
    {
        /// <summary>
        /// Creates a timestamped message that sets the digital output lines specified in the mask to logic HIGH.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DOPortSet register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortSet.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that clears the digital output lines specified in the mask to logic LOW.
    /// </summary>
    [DisplayName("DOPortClearPayload")]
    [Description("Creates a message payload that clears the digital output lines specified in the mask to logic LOW.")]
    public partial class CreateDOPortClearPayload
    {
        /// <summary>
        /// Gets or sets the value that clears the digital output lines specified in the mask to logic LOW.
        /// </summary>
        [Description("The value that clears the digital output lines specified in the mask to logic LOW.")]
        public DigitalOutputs DOPortClear { get; set; }

        /// <summary>
        /// Creates a message payload for the DOPortClear register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalOutputs GetPayload()
        {
            return DOPortClear;
        }

        /// <summary>
        /// Creates a message that clears the digital output lines specified in the mask to logic LOW.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DOPortClear register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortClear.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that clears the digital output lines specified in the mask to logic LOW.
    /// </summary>
    [DisplayName("TimestampedDOPortClearPayload")]
    [Description("Creates a timestamped message payload that clears the digital output lines specified in the mask to logic LOW.")]
    public partial class CreateTimestampedDOPortClearPayload : CreateDOPortClearPayload
    {
        /// <summary>
        /// Creates a timestamped message that clears the digital output lines specified in the mask to logic LOW.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DOPortClear register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DOPortClear.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects the raw state of the external trigger input lines.
    /// </summary>
    [DisplayName("ExternalTriggerStatePayload")]
    [Description("Creates a message payload that reflects the raw state of the external trigger input lines.")]
    public partial class CreateExternalTriggerStatePayload
    {
        /// <summary>
        /// Gets or sets the value that reflects the raw state of the external trigger input lines.
        /// </summary>
        [Description("The value that reflects the raw state of the external trigger input lines.")]
        public DigitalInputs ExternalTriggerState { get; set; }

        /// <summary>
        /// Creates a message payload for the ExternalTriggerState register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalInputs GetPayload()
        {
            return ExternalTriggerState;
        }

        /// <summary>
        /// Creates a message that reflects the raw state of the external trigger input lines.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ExternalTriggerState register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ExternalTriggerState.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects the raw state of the external trigger input lines.
    /// </summary>
    [DisplayName("TimestampedExternalTriggerStatePayload")]
    [Description("Creates a timestamped message payload that reflects the raw state of the external trigger input lines.")]
    public partial class CreateTimestampedExternalTriggerStatePayload : CreateExternalTriggerStatePayload
    {
        /// <summary>
        /// Creates a timestamped message that reflects the raw state of the external trigger input lines.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ExternalTriggerState register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ExternalTriggerState.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.
    /// </summary>
    [DisplayName("AOPortStatePayload")]
    [Description("Creates a message payload that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.")]
    public partial class CreateAOPortStatePayload
    {
        /// <summary>
        /// Gets or sets a value that the output voltage of analog output channel 0 in volts.
        /// </summary>
        [Description("The output voltage of analog output channel 0 in volts.")]
        public float AOChannel0 { get; set; }

        /// <summary>
        /// Gets or sets a value that the output voltage of analog output channel 1 in volts.
        /// </summary>
        [Description("The output voltage of analog output channel 1 in volts.")]
        public float AOChannel1 { get; set; }

        /// <summary>
        /// Gets or sets a value that the output voltage of analog output channel 2 in volts.
        /// </summary>
        [Description("The output voltage of analog output channel 2 in volts.")]
        public float AOChannel2 { get; set; }

        /// <summary>
        /// Gets or sets a value that the output voltage of analog output channel 3 in volts.
        /// </summary>
        [Description("The output voltage of analog output channel 3 in volts.")]
        public float AOChannel3 { get; set; }

        /// <summary>
        /// Creates a message payload for the AOPortState register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AOPortStatePayload GetPayload()
        {
            AOPortStatePayload value;
            value.AOChannel0 = AOChannel0;
            value.AOChannel1 = AOChannel1;
            value.AOChannel2 = AOChannel2;
            value.AOChannel3 = AOChannel3;
            return value;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AOPortState register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOPortState.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.
    /// </summary>
    [DisplayName("TimestampedAOPortStatePayload")]
    [Description("Creates a timestamped message payload that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.")]
    public partial class CreateTimestampedAOPortStatePayload : CreateAOPortStatePayload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the output voltage of all four analog output channels in volts. Reads and writes fail with an error unless every channel is idle, since a channel playing a preset waveform owns its output value.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AOPortState register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOPortState.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("AOChannel0Payload")]
    [Description("Creates a message payload that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateAOChannel0Payload
    {
        /// <summary>
        /// Gets or sets the value that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        [Range(min: -10, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
        public float AOChannel0 { get; set; } = 0F;

        /// <summary>
        /// Creates a message payload for the AOChannel0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public float GetPayload()
        {
            return AOChannel0;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AOChannel0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("TimestampedAOChannel0Payload")]
    [Description("Creates a timestamped message payload that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateTimestampedAOChannel0Payload : CreateAOChannel0Payload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the output voltage of analog output channel 0 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AOChannel0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("AOChannel1Payload")]
    [Description("Creates a message payload that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateAOChannel1Payload
    {
        /// <summary>
        /// Gets or sets the value that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        [Range(min: -10, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
        public float AOChannel1 { get; set; } = 0F;

        /// <summary>
        /// Creates a message payload for the AOChannel1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public float GetPayload()
        {
            return AOChannel1;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AOChannel1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("TimestampedAOChannel1Payload")]
    [Description("Creates a timestamped message payload that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateTimestampedAOChannel1Payload : CreateAOChannel1Payload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the output voltage of analog output channel 1 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AOChannel1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("AOChannel2Payload")]
    [Description("Creates a message payload that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateAOChannel2Payload
    {
        /// <summary>
        /// Gets or sets the value that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        [Range(min: -10, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
        public float AOChannel2 { get; set; } = 0F;

        /// <summary>
        /// Creates a message payload for the AOChannel2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public float GetPayload()
        {
            return AOChannel2;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AOChannel2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("TimestampedAOChannel2Payload")]
    [Description("Creates a timestamped message payload that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateTimestampedAOChannel2Payload : CreateAOChannel2Payload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the output voltage of analog output channel 2 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AOChannel2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("AOChannel3Payload")]
    [Description("Creates a message payload that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateAOChannel3Payload
    {
        /// <summary>
        /// Gets or sets the value that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        [Range(min: -10, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
        public float AOChannel3 { get; set; } = 0F;

        /// <summary>
        /// Creates a message payload for the AOChannel3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public float GetPayload()
        {
            return AOChannel3;
        }

        /// <summary>
        /// Creates a message that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AOChannel3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
    /// </summary>
    [DisplayName("TimestampedAOChannel3Payload")]
    [Description("Creates a timestamped message payload that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.")]
    public partial class CreateTimestampedAOChannel3Payload : CreateAOChannel3Payload
    {
        /// <summary>
        /// Creates a timestamped message that reflects and specifies the output voltage of analog output channel 3 in volts. Writes fail with an error while this channel is playing a preset waveform, and reads fail with an error while any channel is playing a preset waveform.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AOChannel3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.AOChannel3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reflects which analog output channels are configured and ready to start.
    /// </summary>
    [DisplayName("DacReadyPayload")]
    [Description("Creates a message payload that reflects which analog output channels are configured and ready to start.")]
    public partial class CreateDacReadyPayload
    {
        /// <summary>
        /// Gets or sets the value that reflects which analog output channels are configured and ready to start.
        /// </summary>
        [Description("The value that reflects which analog output channels are configured and ready to start.")]
        public AnalogOutputs DacReady { get; set; }

        /// <summary>
        /// Creates a message payload for the DacReady register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogOutputs GetPayload()
        {
            return DacReady;
        }

        /// <summary>
        /// Creates a message that reflects which analog output channels are configured and ready to start.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DacReady register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacReady.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reflects which analog output channels are configured and ready to start.
    /// </summary>
    [DisplayName("TimestampedDacReadyPayload")]
    [Description("Creates a timestamped message payload that reflects which analog output channels are configured and ready to start.")]
    public partial class CreateTimestampedDacReadyPayload : CreateDacReadyPayload
    {
        /// <summary>
        /// Creates a timestamped message that reflects which analog output channels are configured and ready to start.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DacReady register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacReady.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
    /// </summary>
    [DisplayName("DacStartPayload")]
    [Description("Creates a message payload that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.")]
    public partial class CreateDacStartPayload
    {
        /// <summary>
        /// Gets or sets the value that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
        /// </summary>
        [Description("The value that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.")]
        public AnalogOutputs DacStart { get; set; }

        /// <summary>
        /// Creates a message payload for the DacStart register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogOutputs GetPayload()
        {
            return DacStart;
        }

        /// <summary>
        /// Creates a message that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DacStart register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacStart.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
    /// </summary>
    [DisplayName("TimestampedDacStartPayload")]
    [Description("Creates a timestamped message payload that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.")]
    public partial class CreateTimestampedDacStartPayload : CreateDacStartPayload
    {
        /// <summary>
        /// Creates a timestamped message that starts the waveform player on the ready analog output channels specified in the mask. Events from this register report the channels that were started by an external trigger.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DacStart register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacStart.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that pauses the channels set in the mask and resumes those cleared in it.
    /// </summary>
    [DisplayName("DacPausePayload")]
    [Description("Creates a message payload that pauses the channels set in the mask and resumes those cleared in it.")]
    public partial class CreateDacPausePayload
    {
        /// <summary>
        /// Gets or sets the value that pauses the channels set in the mask and resumes those cleared in it.
        /// </summary>
        [Description("The value that pauses the channels set in the mask and resumes those cleared in it.")]
        public AnalogOutputs DacPause { get; set; }

        /// <summary>
        /// Creates a message payload for the DacPause register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogOutputs GetPayload()
        {
            return DacPause;
        }

        /// <summary>
        /// Creates a message that pauses the channels set in the mask and resumes those cleared in it.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DacPause register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacPause.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that pauses the channels set in the mask and resumes those cleared in it.
    /// </summary>
    [DisplayName("TimestampedDacPausePayload")]
    [Description("Creates a timestamped message payload that pauses the channels set in the mask and resumes those cleared in it.")]
    public partial class CreateTimestampedDacPausePayload : CreateDacPausePayload
    {
        /// <summary>
        /// Creates a timestamped message that pauses the channels set in the mask and resumes those cleared in it.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DacPause register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacPause.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that aborts waveform playback on the analog output channels specified in the mask.
    /// </summary>
    [DisplayName("DacAbortPayload")]
    [Description("Creates a message payload that aborts waveform playback on the analog output channels specified in the mask.")]
    public partial class CreateDacAbortPayload
    {
        /// <summary>
        /// Gets or sets the value that aborts waveform playback on the analog output channels specified in the mask.
        /// </summary>
        [Description("The value that aborts waveform playback on the analog output channels specified in the mask.")]
        public AnalogOutputs DacAbort { get; set; }

        /// <summary>
        /// Creates a message payload for the DacAbort register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogOutputs GetPayload()
        {
            return DacAbort;
        }

        /// <summary>
        /// Creates a message that aborts waveform playback on the analog output channels specified in the mask.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DacAbort register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacAbort.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that aborts waveform playback on the analog output channels specified in the mask.
    /// </summary>
    [DisplayName("TimestampedDacAbortPayload")]
    [Description("Creates a timestamped message payload that aborts waveform playback on the analog output channels specified in the mask.")]
    public partial class CreateTimestampedDacAbortPayload : CreateDacAbortPayload
    {
        /// <summary>
        /// Creates a timestamped message that aborts waveform playback on the analog output channels specified in the mask.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DacAbort register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacAbort.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that reports which analog output channels have finished playing their waveform.
    /// </summary>
    [DisplayName("DacFinishedPayload")]
    [Description("Creates a message payload that reports which analog output channels have finished playing their waveform.")]
    public partial class CreateDacFinishedPayload
    {
        /// <summary>
        /// Gets or sets the value that reports which analog output channels have finished playing their waveform.
        /// </summary>
        [Description("The value that reports which analog output channels have finished playing their waveform.")]
        public AnalogOutputs DacFinished { get; set; }

        /// <summary>
        /// Creates a message payload for the DacFinished register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogOutputs GetPayload()
        {
            return DacFinished;
        }

        /// <summary>
        /// Creates a message that reports which analog output channels have finished playing their waveform.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DacFinished register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacFinished.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that reports which analog output channels have finished playing their waveform.
    /// </summary>
    [DisplayName("TimestampedDacFinishedPayload")]
    [Description("Creates a timestamped message payload that reports which analog output channels have finished playing their waveform.")]
    public partial class CreateTimestampedDacFinishedPayload : CreateDacFinishedPayload
    {
        /// <summary>
        /// Creates a timestamped message that reports which analog output channels have finished playing their waveform.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DacFinished register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.DacFinished.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which external trigger lines can start channel 0.
    /// </summary>
    [DisplayName("ChannelExternalTriggers0Payload")]
    [Description("Creates a message payload that specifies which external trigger lines can start channel 0.")]
    public partial class CreateChannelExternalTriggers0Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which external trigger lines can start channel 0.
        /// </summary>
        [Description("The value that specifies which external trigger lines can start channel 0.")]
        public DigitalInputs ChannelExternalTriggers0 { get; set; }

        /// <summary>
        /// Creates a message payload for the ChannelExternalTriggers0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalInputs GetPayload()
        {
            return ChannelExternalTriggers0;
        }

        /// <summary>
        /// Creates a message that specifies which external trigger lines can start channel 0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ChannelExternalTriggers0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which external trigger lines can start channel 0.
    /// </summary>
    [DisplayName("TimestampedChannelExternalTriggers0Payload")]
    [Description("Creates a timestamped message payload that specifies which external trigger lines can start channel 0.")]
    public partial class CreateTimestampedChannelExternalTriggers0Payload : CreateChannelExternalTriggers0Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which external trigger lines can start channel 0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ChannelExternalTriggers0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which external trigger lines can start channel 1.
    /// </summary>
    [DisplayName("ChannelExternalTriggers1Payload")]
    [Description("Creates a message payload that specifies which external trigger lines can start channel 1.")]
    public partial class CreateChannelExternalTriggers1Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which external trigger lines can start channel 1.
        /// </summary>
        [Description("The value that specifies which external trigger lines can start channel 1.")]
        public DigitalInputs ChannelExternalTriggers1 { get; set; }

        /// <summary>
        /// Creates a message payload for the ChannelExternalTriggers1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalInputs GetPayload()
        {
            return ChannelExternalTriggers1;
        }

        /// <summary>
        /// Creates a message that specifies which external trigger lines can start channel 1.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ChannelExternalTriggers1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which external trigger lines can start channel 1.
    /// </summary>
    [DisplayName("TimestampedChannelExternalTriggers1Payload")]
    [Description("Creates a timestamped message payload that specifies which external trigger lines can start channel 1.")]
    public partial class CreateTimestampedChannelExternalTriggers1Payload : CreateChannelExternalTriggers1Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which external trigger lines can start channel 1.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ChannelExternalTriggers1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which external trigger lines can start channel 2.
    /// </summary>
    [DisplayName("ChannelExternalTriggers2Payload")]
    [Description("Creates a message payload that specifies which external trigger lines can start channel 2.")]
    public partial class CreateChannelExternalTriggers2Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which external trigger lines can start channel 2.
        /// </summary>
        [Description("The value that specifies which external trigger lines can start channel 2.")]
        public DigitalInputs ChannelExternalTriggers2 { get; set; }

        /// <summary>
        /// Creates a message payload for the ChannelExternalTriggers2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalInputs GetPayload()
        {
            return ChannelExternalTriggers2;
        }

        /// <summary>
        /// Creates a message that specifies which external trigger lines can start channel 2.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ChannelExternalTriggers2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which external trigger lines can start channel 2.
    /// </summary>
    [DisplayName("TimestampedChannelExternalTriggers2Payload")]
    [Description("Creates a timestamped message payload that specifies which external trigger lines can start channel 2.")]
    public partial class CreateTimestampedChannelExternalTriggers2Payload : CreateChannelExternalTriggers2Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which external trigger lines can start channel 2.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ChannelExternalTriggers2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which external trigger lines can start channel 3.
    /// </summary>
    [DisplayName("ChannelExternalTriggers3Payload")]
    [Description("Creates a message payload that specifies which external trigger lines can start channel 3.")]
    public partial class CreateChannelExternalTriggers3Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which external trigger lines can start channel 3.
        /// </summary>
        [Description("The value that specifies which external trigger lines can start channel 3.")]
        public DigitalInputs ChannelExternalTriggers3 { get; set; }

        /// <summary>
        /// Creates a message payload for the ChannelExternalTriggers3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public DigitalInputs GetPayload()
        {
            return ChannelExternalTriggers3;
        }

        /// <summary>
        /// Creates a message that specifies which external trigger lines can start channel 3.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ChannelExternalTriggers3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which external trigger lines can start channel 3.
    /// </summary>
    [DisplayName("TimestampedChannelExternalTriggers3Payload")]
    [Description("Creates a timestamped message payload that specifies which external trigger lines can start channel 3.")]
    public partial class CreateTimestampedChannelExternalTriggers3Payload : CreateChannelExternalTriggers3Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which external trigger lines can start channel 3.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ChannelExternalTriggers3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ChannelExternalTriggers3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which waveform player drives analog output channel 0.
    /// </summary>
    [DisplayName("ActivePlayer0Payload")]
    [Description("Creates a message payload that specifies which waveform player drives analog output channel 0.")]
    public partial class CreateActivePlayer0Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which waveform player drives analog output channel 0.
        /// </summary>
        [Description("The value that specifies which waveform player drives analog output channel 0.")]
        public PlayerType ActivePlayer0 { get; set; }

        /// <summary>
        /// Creates a message payload for the ActivePlayer0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public PlayerType GetPayload()
        {
            return ActivePlayer0;
        }

        /// <summary>
        /// Creates a message that specifies which waveform player drives analog output channel 0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ActivePlayer0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which waveform player drives analog output channel 0.
    /// </summary>
    [DisplayName("TimestampedActivePlayer0Payload")]
    [Description("Creates a timestamped message payload that specifies which waveform player drives analog output channel 0.")]
    public partial class CreateTimestampedActivePlayer0Payload : CreateActivePlayer0Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which waveform player drives analog output channel 0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ActivePlayer0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which waveform player drives analog output channel 1.
    /// </summary>
    [DisplayName("ActivePlayer1Payload")]
    [Description("Creates a message payload that specifies which waveform player drives analog output channel 1.")]
    public partial class CreateActivePlayer1Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which waveform player drives analog output channel 1.
        /// </summary>
        [Description("The value that specifies which waveform player drives analog output channel 1.")]
        public PlayerType ActivePlayer1 { get; set; }

        /// <summary>
        /// Creates a message payload for the ActivePlayer1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public PlayerType GetPayload()
        {
            return ActivePlayer1;
        }

        /// <summary>
        /// Creates a message that specifies which waveform player drives analog output channel 1.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ActivePlayer1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which waveform player drives analog output channel 1.
    /// </summary>
    [DisplayName("TimestampedActivePlayer1Payload")]
    [Description("Creates a timestamped message payload that specifies which waveform player drives analog output channel 1.")]
    public partial class CreateTimestampedActivePlayer1Payload : CreateActivePlayer1Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which waveform player drives analog output channel 1.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ActivePlayer1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which waveform player drives analog output channel 2.
    /// </summary>
    [DisplayName("ActivePlayer2Payload")]
    [Description("Creates a message payload that specifies which waveform player drives analog output channel 2.")]
    public partial class CreateActivePlayer2Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which waveform player drives analog output channel 2.
        /// </summary>
        [Description("The value that specifies which waveform player drives analog output channel 2.")]
        public PlayerType ActivePlayer2 { get; set; }

        /// <summary>
        /// Creates a message payload for the ActivePlayer2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public PlayerType GetPayload()
        {
            return ActivePlayer2;
        }

        /// <summary>
        /// Creates a message that specifies which waveform player drives analog output channel 2.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ActivePlayer2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which waveform player drives analog output channel 2.
    /// </summary>
    [DisplayName("TimestampedActivePlayer2Payload")]
    [Description("Creates a timestamped message payload that specifies which waveform player drives analog output channel 2.")]
    public partial class CreateTimestampedActivePlayer2Payload : CreateActivePlayer2Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which waveform player drives analog output channel 2.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ActivePlayer2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies which waveform player drives analog output channel 3.
    /// </summary>
    [DisplayName("ActivePlayer3Payload")]
    [Description("Creates a message payload that specifies which waveform player drives analog output channel 3.")]
    public partial class CreateActivePlayer3Payload
    {
        /// <summary>
        /// Gets or sets the value that specifies which waveform player drives analog output channel 3.
        /// </summary>
        [Description("The value that specifies which waveform player drives analog output channel 3.")]
        public PlayerType ActivePlayer3 { get; set; }

        /// <summary>
        /// Creates a message payload for the ActivePlayer3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public PlayerType GetPayload()
        {
            return ActivePlayer3;
        }

        /// <summary>
        /// Creates a message that specifies which waveform player drives analog output channel 3.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ActivePlayer3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies which waveform player drives analog output channel 3.
    /// </summary>
    [DisplayName("TimestampedActivePlayer3Payload")]
    [Description("Creates a timestamped message payload that specifies which waveform player drives analog output channel 3.")]
    public partial class CreateTimestampedActivePlayer3Payload : CreateActivePlayer3Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies which waveform player drives analog output channel 3.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ActivePlayer3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.ActivePlayer3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the file player settings for analog output channel 0.
    /// </summary>
    [DisplayName("FileSettings0Payload")]
    [Description("Creates a message payload that specifies the file player settings for analog output channel 0.")]
    public partial class CreateFileSettings0Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        [Description("Specifies the null-terminated path of the waveform file on the SD card.")]
        public string Path { get; set; }

        /// <summary>
        /// Creates a message payload for the FileSettings0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public FileSettings0Payload GetPayload()
        {
            FileSettings0Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Path = Path;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the file player settings for analog output channel 0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the FileSettings0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the file player settings for analog output channel 0.
    /// </summary>
    [DisplayName("TimestampedFileSettings0Payload")]
    [Description("Creates a timestamped message payload that specifies the file player settings for analog output channel 0.")]
    public partial class CreateTimestampedFileSettings0Payload : CreateFileSettings0Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the file player settings for analog output channel 0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the FileSettings0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the file player settings for analog output channel 1.
    /// </summary>
    [DisplayName("FileSettings1Payload")]
    [Description("Creates a message payload that specifies the file player settings for analog output channel 1.")]
    public partial class CreateFileSettings1Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        [Description("Specifies the null-terminated path of the waveform file on the SD card.")]
        public string Path { get; set; }

        /// <summary>
        /// Creates a message payload for the FileSettings1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public FileSettings1Payload GetPayload()
        {
            FileSettings1Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Path = Path;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the file player settings for analog output channel 1.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the FileSettings1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the file player settings for analog output channel 1.
    /// </summary>
    [DisplayName("TimestampedFileSettings1Payload")]
    [Description("Creates a timestamped message payload that specifies the file player settings for analog output channel 1.")]
    public partial class CreateTimestampedFileSettings1Payload : CreateFileSettings1Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the file player settings for analog output channel 1.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the FileSettings1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the file player settings for analog output channel 2.
    /// </summary>
    [DisplayName("FileSettings2Payload")]
    [Description("Creates a message payload that specifies the file player settings for analog output channel 2.")]
    public partial class CreateFileSettings2Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        [Description("Specifies the null-terminated path of the waveform file on the SD card.")]
        public string Path { get; set; }

        /// <summary>
        /// Creates a message payload for the FileSettings2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public FileSettings2Payload GetPayload()
        {
            FileSettings2Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Path = Path;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the file player settings for analog output channel 2.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the FileSettings2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the file player settings for analog output channel 2.
    /// </summary>
    [DisplayName("TimestampedFileSettings2Payload")]
    [Description("Creates a timestamped message payload that specifies the file player settings for analog output channel 2.")]
    public partial class CreateTimestampedFileSettings2Payload : CreateFileSettings2Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the file player settings for analog output channel 2.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the FileSettings2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the file player settings for analog output channel 3.
    /// </summary>
    [DisplayName("FileSettings3Payload")]
    [Description("Creates a message payload that specifies the file player settings for analog output channel 3.")]
    public partial class CreateFileSettings3Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        [Description("Specifies the null-terminated path of the waveform file on the SD card.")]
        public string Path { get; set; }

        /// <summary>
        /// Creates a message payload for the FileSettings3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public FileSettings3Payload GetPayload()
        {
            FileSettings3Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Path = Path;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the file player settings for analog output channel 3.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the FileSettings3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the file player settings for analog output channel 3.
    /// </summary>
    [DisplayName("TimestampedFileSettings3Payload")]
    [Description("Creates a timestamped message payload that specifies the file player settings for analog output channel 3.")]
    public partial class CreateTimestampedFileSettings3Payload : CreateFileSettings3Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the file player settings for analog output channel 3.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the FileSettings3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.FileSettings3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the sine player settings for analog output channel 0.
    /// </summary>
    [DisplayName("SineSettings0Payload")]
    [Description("Creates a message payload that specifies the sine player settings for analog output channel 0.")]
    public partial class CreateSineSettings0Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the SineSettings0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public SineSettings0Payload GetPayload()
        {
            SineSettings0Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the sine player settings for analog output channel 0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the SineSettings0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the sine player settings for analog output channel 0.
    /// </summary>
    [DisplayName("TimestampedSineSettings0Payload")]
    [Description("Creates a timestamped message payload that specifies the sine player settings for analog output channel 0.")]
    public partial class CreateTimestampedSineSettings0Payload : CreateSineSettings0Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the sine player settings for analog output channel 0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the SineSettings0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the sine player settings for analog output channel 1.
    /// </summary>
    [DisplayName("SineSettings1Payload")]
    [Description("Creates a message payload that specifies the sine player settings for analog output channel 1.")]
    public partial class CreateSineSettings1Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the SineSettings1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public SineSettings1Payload GetPayload()
        {
            SineSettings1Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the sine player settings for analog output channel 1.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the SineSettings1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the sine player settings for analog output channel 1.
    /// </summary>
    [DisplayName("TimestampedSineSettings1Payload")]
    [Description("Creates a timestamped message payload that specifies the sine player settings for analog output channel 1.")]
    public partial class CreateTimestampedSineSettings1Payload : CreateSineSettings1Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the sine player settings for analog output channel 1.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the SineSettings1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the sine player settings for analog output channel 2.
    /// </summary>
    [DisplayName("SineSettings2Payload")]
    [Description("Creates a message payload that specifies the sine player settings for analog output channel 2.")]
    public partial class CreateSineSettings2Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the SineSettings2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public SineSettings2Payload GetPayload()
        {
            SineSettings2Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the sine player settings for analog output channel 2.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the SineSettings2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the sine player settings for analog output channel 2.
    /// </summary>
    [DisplayName("TimestampedSineSettings2Payload")]
    [Description("Creates a timestamped message payload that specifies the sine player settings for analog output channel 2.")]
    public partial class CreateTimestampedSineSettings2Payload : CreateSineSettings2Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the sine player settings for analog output channel 2.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the SineSettings2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the sine player settings for analog output channel 3.
    /// </summary>
    [DisplayName("SineSettings3Payload")]
    [Description("Creates a message payload that specifies the sine player settings for analog output channel 3.")]
    public partial class CreateSineSettings3Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the SineSettings3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public SineSettings3Payload GetPayload()
        {
            SineSettings3Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the sine player settings for analog output channel 3.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the SineSettings3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the sine player settings for analog output channel 3.
    /// </summary>
    [DisplayName("TimestampedSineSettings3Payload")]
    [Description("Creates a timestamped message payload that specifies the sine player settings for analog output channel 3.")]
    public partial class CreateTimestampedSineSettings3Payload : CreateSineSettings3Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the sine player settings for analog output channel 3.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the SineSettings3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.SineSettings3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the trapezoid player settings for analog output channel 0.
    /// </summary>
    [DisplayName("TrapezoidSettings0Payload")]
    [Description("Creates a message payload that specifies the trapezoid player settings for analog output channel 0.")]
    public partial class CreateTrapezoidSettings0Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the rising ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the rising ramp in microseconds.")]
        public uint RampOnDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        [Description("Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.")]
        public uint PulseWidthDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the falling ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the falling ramp in microseconds.")]
        public uint RampOffDuration { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the TrapezoidSettings0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public TrapezoidSettings0Payload GetPayload()
        {
            TrapezoidSettings0Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            value.RampOnDuration = RampOnDuration;
            value.PulseWidthDuration = PulseWidthDuration;
            value.RampOffDuration = RampOffDuration;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the trapezoid player settings for analog output channel 0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the TrapezoidSettings0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the trapezoid player settings for analog output channel 0.
    /// </summary>
    [DisplayName("TimestampedTrapezoidSettings0Payload")]
    [Description("Creates a timestamped message payload that specifies the trapezoid player settings for analog output channel 0.")]
    public partial class CreateTimestampedTrapezoidSettings0Payload : CreateTrapezoidSettings0Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the trapezoid player settings for analog output channel 0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the TrapezoidSettings0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the trapezoid player settings for analog output channel 1.
    /// </summary>
    [DisplayName("TrapezoidSettings1Payload")]
    [Description("Creates a message payload that specifies the trapezoid player settings for analog output channel 1.")]
    public partial class CreateTrapezoidSettings1Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the rising ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the rising ramp in microseconds.")]
        public uint RampOnDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        [Description("Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.")]
        public uint PulseWidthDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the falling ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the falling ramp in microseconds.")]
        public uint RampOffDuration { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the TrapezoidSettings1 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public TrapezoidSettings1Payload GetPayload()
        {
            TrapezoidSettings1Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            value.RampOnDuration = RampOnDuration;
            value.PulseWidthDuration = PulseWidthDuration;
            value.RampOffDuration = RampOffDuration;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the trapezoid player settings for analog output channel 1.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the TrapezoidSettings1 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings1.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the trapezoid player settings for analog output channel 1.
    /// </summary>
    [DisplayName("TimestampedTrapezoidSettings1Payload")]
    [Description("Creates a timestamped message payload that specifies the trapezoid player settings for analog output channel 1.")]
    public partial class CreateTimestampedTrapezoidSettings1Payload : CreateTrapezoidSettings1Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the trapezoid player settings for analog output channel 1.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the TrapezoidSettings1 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings1.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the trapezoid player settings for analog output channel 2.
    /// </summary>
    [DisplayName("TrapezoidSettings2Payload")]
    [Description("Creates a message payload that specifies the trapezoid player settings for analog output channel 2.")]
    public partial class CreateTrapezoidSettings2Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the rising ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the rising ramp in microseconds.")]
        public uint RampOnDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        [Description("Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.")]
        public uint PulseWidthDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the falling ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the falling ramp in microseconds.")]
        public uint RampOffDuration { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the TrapezoidSettings2 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public TrapezoidSettings2Payload GetPayload()
        {
            TrapezoidSettings2Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            value.RampOnDuration = RampOnDuration;
            value.PulseWidthDuration = PulseWidthDuration;
            value.RampOffDuration = RampOffDuration;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the trapezoid player settings for analog output channel 2.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the TrapezoidSettings2 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings2.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the trapezoid player settings for analog output channel 2.
    /// </summary>
    [DisplayName("TimestampedTrapezoidSettings2Payload")]
    [Description("Creates a timestamped message payload that specifies the trapezoid player settings for analog output channel 2.")]
    public partial class CreateTimestampedTrapezoidSettings2Payload : CreateTrapezoidSettings2Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the trapezoid player settings for analog output channel 2.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the TrapezoidSettings2 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings2.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// that specifies the trapezoid player settings for analog output channel 3.
    /// </summary>
    [DisplayName("TrapezoidSettings3Payload")]
    [Description("Creates a message payload that specifies the trapezoid player settings for analog output channel 3.")]
    public partial class CreateTrapezoidSettings3Payload
    {
        /// <summary>
        /// Gets or sets a value that specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        [Description("Specifies how many times the waveform is played, or zero to play it indefinitely.")]
        public uint Cycles { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        [Description("Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.")]
        public uint Duration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the sample update rate in hertz.
        /// </summary>
        [Description("Specifies the sample update rate in hertz.")]
        public uint UpdateFrequency { get; set; } = 500000;

        /// <summary>
        /// Gets or sets a value that specifies the frequency of the waveform in hertz.
        /// </summary>
        [Description("Specifies the frequency of the waveform in hertz.")]
        public uint Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        [Range(min: 0, max: 10)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.")]
        public float Amplitude { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        [Description("Specifies the vertical offset applied to the waveform in volts.")]
        public float VerticalShift { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value that normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        [Range(min: -1, max: 1)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).")]
        public float NormalizedPhaseShift { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the rising ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the rising ramp in microseconds.")]
        public uint RampOnDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        [Description("Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.")]
        public uint PulseWidthDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value that specifies the duration of the falling ramp in microseconds.
        /// </summary>
        [Description("Specifies the duration of the falling ramp in microseconds.")]
        public uint RampOffDuration { get; set; } = 0;

        /// <summary>
        /// Creates a message payload for the TrapezoidSettings3 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public TrapezoidSettings3Payload GetPayload()
        {
            TrapezoidSettings3Payload value;
            value.Cycles = Cycles;
            value.Duration = Duration;
            value.UpdateFrequency = UpdateFrequency;
            value.Frequency = Frequency;
            value.Amplitude = Amplitude;
            value.VerticalShift = VerticalShift;
            value.NormalizedPhaseShift = NormalizedPhaseShift;
            value.RampOnDuration = RampOnDuration;
            value.PulseWidthDuration = PulseWidthDuration;
            value.RampOffDuration = RampOffDuration;
            return value;
        }

        /// <summary>
        /// Creates a message that specifies the trapezoid player settings for analog output channel 3.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the TrapezoidSettings3 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings3.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// that specifies the trapezoid player settings for analog output channel 3.
    /// </summary>
    [DisplayName("TimestampedTrapezoidSettings3Payload")]
    [Description("Creates a timestamped message payload that specifies the trapezoid player settings for analog output channel 3.")]
    public partial class CreateTimestampedTrapezoidSettings3Payload : CreateTrapezoidSettings3Payload
    {
        /// <summary>
        /// Creates a timestamped message that specifies the trapezoid player settings for analog output channel 3.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the TrapezoidSettings3 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return AllenNeuralDynamics.Quac.TrapezoidSettings3.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents the payload of the AOPortState register.
    /// </summary>
    public struct AOPortStatePayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AOPortStatePayload"/> structure.
        /// </summary>
        /// <param name="aOChannel0">The output voltage of analog output channel 0 in volts.</param>
        /// <param name="aOChannel1">The output voltage of analog output channel 1 in volts.</param>
        /// <param name="aOChannel2">The output voltage of analog output channel 2 in volts.</param>
        /// <param name="aOChannel3">The output voltage of analog output channel 3 in volts.</param>
        public AOPortStatePayload(
            float aOChannel0,
            float aOChannel1,
            float aOChannel2,
            float aOChannel3)
        {
            AOChannel0 = aOChannel0;
            AOChannel1 = aOChannel1;
            AOChannel2 = aOChannel2;
            AOChannel3 = aOChannel3;
        }

        /// <summary>
        /// The output voltage of analog output channel 0 in volts.
        /// </summary>
        public float AOChannel0;

        /// <summary>
        /// The output voltage of analog output channel 1 in volts.
        /// </summary>
        public float AOChannel1;

        /// <summary>
        /// The output voltage of analog output channel 2 in volts.
        /// </summary>
        public float AOChannel2;

        /// <summary>
        /// The output voltage of analog output channel 3 in volts.
        /// </summary>
        public float AOChannel3;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the AOPortState register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// AOPortState register.
        /// </returns>
        public override string ToString()
        {
            return "AOPortStatePayload { " +
                "AOChannel0 = " + AOChannel0 + ", " +
                "AOChannel1 = " + AOChannel1 + ", " +
                "AOChannel2 = " + AOChannel2 + ", " +
                "AOChannel3 = " + AOChannel3 + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the FileSettings0 register.
    /// </summary>
    public struct FileSettings0Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSettings0Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="path">Specifies the null-terminated path of the waveform file on the SD card.</param>
        public FileSettings0Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            string path)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Path = path;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        public string Path;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the FileSettings0 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// FileSettings0 register.
        /// </returns>
        public override string ToString()
        {
            return "FileSettings0Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Path = " + Path + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the FileSettings1 register.
    /// </summary>
    public struct FileSettings1Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSettings1Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="path">Specifies the null-terminated path of the waveform file on the SD card.</param>
        public FileSettings1Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            string path)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Path = path;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        public string Path;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the FileSettings1 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// FileSettings1 register.
        /// </returns>
        public override string ToString()
        {
            return "FileSettings1Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Path = " + Path + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the FileSettings2 register.
    /// </summary>
    public struct FileSettings2Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSettings2Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="path">Specifies the null-terminated path of the waveform file on the SD card.</param>
        public FileSettings2Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            string path)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Path = path;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        public string Path;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the FileSettings2 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// FileSettings2 register.
        /// </returns>
        public override string ToString()
        {
            return "FileSettings2Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Path = " + Path + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the FileSettings3 register.
    /// </summary>
    public struct FileSettings3Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSettings3Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="path">Specifies the null-terminated path of the waveform file on the SD card.</param>
        public FileSettings3Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            string path)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Path = path;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the null-terminated path of the waveform file on the SD card.
        /// </summary>
        public string Path;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the FileSettings3 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// FileSettings3 register.
        /// </returns>
        public override string ToString()
        {
            return "FileSettings3Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Path = " + Path + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the SineSettings0 register.
    /// </summary>
    public struct SineSettings0Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SineSettings0Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        public SineSettings0Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the SineSettings0 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// SineSettings0 register.
        /// </returns>
        public override string ToString()
        {
            return "SineSettings0Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the SineSettings1 register.
    /// </summary>
    public struct SineSettings1Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SineSettings1Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        public SineSettings1Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the SineSettings1 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// SineSettings1 register.
        /// </returns>
        public override string ToString()
        {
            return "SineSettings1Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the SineSettings2 register.
    /// </summary>
    public struct SineSettings2Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SineSettings2Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        public SineSettings2Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the SineSettings2 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// SineSettings2 register.
        /// </returns>
        public override string ToString()
        {
            return "SineSettings2Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the SineSettings3 register.
    /// </summary>
    public struct SineSettings3Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SineSettings3Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        public SineSettings3Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the SineSettings3 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// SineSettings3 register.
        /// </returns>
        public override string ToString()
        {
            return "SineSettings3Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the TrapezoidSettings0 register.
    /// </summary>
    public struct TrapezoidSettings0Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrapezoidSettings0Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        /// <param name="rampOnDuration">Specifies the duration of the rising ramp in microseconds.</param>
        /// <param name="pulseWidthDuration">Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.</param>
        /// <param name="rampOffDuration">Specifies the duration of the falling ramp in microseconds.</param>
        public TrapezoidSettings0Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift,
            uint rampOnDuration,
            uint pulseWidthDuration,
            uint rampOffDuration)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
            RampOnDuration = rampOnDuration;
            PulseWidthDuration = pulseWidthDuration;
            RampOffDuration = rampOffDuration;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Specifies the duration of the rising ramp in microseconds.
        /// </summary>
        public uint RampOnDuration;

        /// <summary>
        /// Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        public uint PulseWidthDuration;

        /// <summary>
        /// Specifies the duration of the falling ramp in microseconds.
        /// </summary>
        public uint RampOffDuration;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the TrapezoidSettings0 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// TrapezoidSettings0 register.
        /// </returns>
        public override string ToString()
        {
            return "TrapezoidSettings0Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + ", " +
                "RampOnDuration = " + RampOnDuration + ", " +
                "PulseWidthDuration = " + PulseWidthDuration + ", " +
                "RampOffDuration = " + RampOffDuration + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the TrapezoidSettings1 register.
    /// </summary>
    public struct TrapezoidSettings1Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrapezoidSettings1Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        /// <param name="rampOnDuration">Specifies the duration of the rising ramp in microseconds.</param>
        /// <param name="pulseWidthDuration">Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.</param>
        /// <param name="rampOffDuration">Specifies the duration of the falling ramp in microseconds.</param>
        public TrapezoidSettings1Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift,
            uint rampOnDuration,
            uint pulseWidthDuration,
            uint rampOffDuration)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
            RampOnDuration = rampOnDuration;
            PulseWidthDuration = pulseWidthDuration;
            RampOffDuration = rampOffDuration;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Specifies the duration of the rising ramp in microseconds.
        /// </summary>
        public uint RampOnDuration;

        /// <summary>
        /// Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        public uint PulseWidthDuration;

        /// <summary>
        /// Specifies the duration of the falling ramp in microseconds.
        /// </summary>
        public uint RampOffDuration;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the TrapezoidSettings1 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// TrapezoidSettings1 register.
        /// </returns>
        public override string ToString()
        {
            return "TrapezoidSettings1Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + ", " +
                "RampOnDuration = " + RampOnDuration + ", " +
                "PulseWidthDuration = " + PulseWidthDuration + ", " +
                "RampOffDuration = " + RampOffDuration + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the TrapezoidSettings2 register.
    /// </summary>
    public struct TrapezoidSettings2Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrapezoidSettings2Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        /// <param name="rampOnDuration">Specifies the duration of the rising ramp in microseconds.</param>
        /// <param name="pulseWidthDuration">Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.</param>
        /// <param name="rampOffDuration">Specifies the duration of the falling ramp in microseconds.</param>
        public TrapezoidSettings2Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift,
            uint rampOnDuration,
            uint pulseWidthDuration,
            uint rampOffDuration)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
            RampOnDuration = rampOnDuration;
            PulseWidthDuration = pulseWidthDuration;
            RampOffDuration = rampOffDuration;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Specifies the duration of the rising ramp in microseconds.
        /// </summary>
        public uint RampOnDuration;

        /// <summary>
        /// Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        public uint PulseWidthDuration;

        /// <summary>
        /// Specifies the duration of the falling ramp in microseconds.
        /// </summary>
        public uint RampOffDuration;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the TrapezoidSettings2 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// TrapezoidSettings2 register.
        /// </returns>
        public override string ToString()
        {
            return "TrapezoidSettings2Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + ", " +
                "RampOnDuration = " + RampOnDuration + ", " +
                "PulseWidthDuration = " + PulseWidthDuration + ", " +
                "RampOffDuration = " + RampOffDuration + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the TrapezoidSettings3 register.
    /// </summary>
    public struct TrapezoidSettings3Payload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrapezoidSettings3Payload"/> structure.
        /// </summary>
        /// <param name="cycles">Specifies how many times the waveform is played, or zero to play it indefinitely.</param>
        /// <param name="duration">Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.</param>
        /// <param name="updateFrequency">Specifies the sample update rate in hertz.</param>
        /// <param name="frequency">Specifies the frequency of the waveform in hertz.</param>
        /// <param name="amplitude">Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.</param>
        /// <param name="verticalShift">Specifies the vertical offset applied to the waveform in volts.</param>
        /// <param name="normalizedPhaseShift">Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).</param>
        /// <param name="rampOnDuration">Specifies the duration of the rising ramp in microseconds.</param>
        /// <param name="pulseWidthDuration">Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.</param>
        /// <param name="rampOffDuration">Specifies the duration of the falling ramp in microseconds.</param>
        public TrapezoidSettings3Payload(
            uint cycles,
            uint duration,
            uint updateFrequency,
            uint frequency,
            float amplitude,
            float verticalShift,
            float normalizedPhaseShift,
            uint rampOnDuration,
            uint pulseWidthDuration,
            uint rampOffDuration)
        {
            Cycles = cycles;
            Duration = duration;
            UpdateFrequency = updateFrequency;
            Frequency = frequency;
            Amplitude = amplitude;
            VerticalShift = verticalShift;
            NormalizedPhaseShift = normalizedPhaseShift;
            RampOnDuration = rampOnDuration;
            PulseWidthDuration = pulseWidthDuration;
            RampOffDuration = rampOffDuration;
        }

        /// <summary>
        /// Specifies how many times the waveform is played, or zero to play it indefinitely.
        /// </summary>
        public uint Cycles;

        /// <summary>
        /// Specifies the duration of a single cycle in microseconds, or zero to play the whole waveform.
        /// </summary>
        public uint Duration;

        /// <summary>
        /// Specifies the sample update rate in hertz.
        /// </summary>
        public uint UpdateFrequency;

        /// <summary>
        /// Specifies the frequency of the waveform in hertz.
        /// </summary>
        public uint Frequency;

        /// <summary>
        /// Specifies the peak amplitude of the waveform in volts, measured from its vertical centre.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// Specifies the vertical offset applied to the waveform in volts.
        /// </summary>
        public float VerticalShift;

        /// <summary>
        /// Normalized phase shift relative to the period with range: -1.0 (max right shift) to 1.0 (max left shift).
        /// </summary>
        public float NormalizedPhaseShift;

        /// <summary>
        /// Specifies the duration of the rising ramp in microseconds.
        /// </summary>
        public uint RampOnDuration;

        /// <summary>
        /// Specifies the total pulse width (including ramp-on and ramp-off duration) in microseconds.
        /// </summary>
        public uint PulseWidthDuration;

        /// <summary>
        /// Specifies the duration of the falling ramp in microseconds.
        /// </summary>
        public uint RampOffDuration;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the TrapezoidSettings3 register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// TrapezoidSettings3 register.
        /// </returns>
        public override string ToString()
        {
            return "TrapezoidSettings3Payload { " +
                "Cycles = " + Cycles + ", " +
                "Duration = " + Duration + ", " +
                "UpdateFrequency = " + UpdateFrequency + ", " +
                "Frequency = " + Frequency + ", " +
                "Amplitude = " + Amplitude + ", " +
                "VerticalShift = " + VerticalShift + ", " +
                "NormalizedPhaseShift = " + NormalizedPhaseShift + ", " +
                "RampOnDuration = " + RampOnDuration + ", " +
                "PulseWidthDuration = " + PulseWidthDuration + ", " +
                "RampOffDuration = " + RampOffDuration + " " +
            "}";
        }
    }

    /// <summary>
    /// Specifies the external trigger input lines available on the device.
    /// </summary>
    [Flags]
    public enum DigitalInputs : byte
    {
        None = 0x0,

        /// <summary>
        /// External trigger input line 0.
        /// </summary>
        [Description("External trigger input line 0.")]
        DI0 = 0x1,

        /// <summary>
        /// External trigger input line 1.
        /// </summary>
        [Description("External trigger input line 1.")]
        DI1 = 0x2,

        /// <summary>
        /// External trigger input line 2.
        /// </summary>
        [Description("External trigger input line 2.")]
        DI2 = 0x4,

        /// <summary>
        /// External trigger input line 3.
        /// </summary>
        [Description("External trigger input line 3.")]
        DI3 = 0x8
    }

    /// <summary>
    /// Specifies the digital output lines available on the device.
    /// </summary>
    [Flags]
    public enum DigitalOutputs : byte
    {
        None = 0x0,

        /// <summary>
        /// Digital output line 0.
        /// </summary>
        [Description("Digital output line 0.")]
        DO0 = 0x1,

        /// <summary>
        /// Digital output line 1.
        /// </summary>
        [Description("Digital output line 1.")]
        DO1 = 0x2,

        /// <summary>
        /// Digital output line 2.
        /// </summary>
        [Description("Digital output line 2.")]
        DO2 = 0x4,

        /// <summary>
        /// Digital output line 3.
        /// </summary>
        [Description("Digital output line 3.")]
        DO3 = 0x8
    }

    /// <summary>
    /// Specifies the ten volt bipolar analog output channels available on the device.
    /// </summary>
    [Flags]
    public enum AnalogOutputs : byte
    {
        None = 0x0,

        /// <summary>
        /// Analog output channel 0.
        /// </summary>
        [Description("Analog output channel 0.")]
        AO0 = 0x1,

        /// <summary>
        /// Analog output channel 1.
        /// </summary>
        [Description("Analog output channel 1.")]
        AO1 = 0x2,

        /// <summary>
        /// Analog output channel 2.
        /// </summary>
        [Description("Analog output channel 2.")]
        AO2 = 0x4,

        /// <summary>
        /// Analog output channel 3.
        /// </summary>
        [Description("Analog output channel 3.")]
        AO3 = 0x8
    }

    /// <summary>
    /// Specifies the waveform player driving an analog output channel.
    /// </summary>
    public enum PlayerType : byte
    {
        /// <summary>
        /// Plays samples streamed from a file on the SD card.
        /// </summary>
        [Description("Plays samples streamed from a file on the SD card.")]
        File = 0,

        /// <summary>
        /// Plays a synthesized sine waveform.
        /// </summary>
        [Description("Plays a synthesized sine waveform.")]
        Sine = 1,

        /// <summary>
        /// Plays a synthesized trapezoid waveform.
        /// </summary>
        [Description("Plays a synthesized trapezoid waveform.")]
        Trapezoid = 2
    }

    internal static partial class PayloadMarshal
    {
        internal static T[] GetSubArray<T>(T[] array, int offset, int count)
        {
            var result = new T[count];
            Array.Copy(array, offset, result, 0, count);
            return result;
        }

        internal static byte ReadByte(ArraySegment<byte> segment) => segment.Array[segment.Offset];

        internal static sbyte ReadSByte(ArraySegment<byte> segment) => (sbyte)segment.Array[segment.Offset];

        internal static ushort ReadUInt16(ArraySegment<byte> segment) => BitConverter.ToUInt16(segment.Array, segment.Offset);

        internal static short ReadInt16(ArraySegment<byte> segment) => BitConverter.ToInt16(segment.Array, segment.Offset);

        internal static uint ReadUInt32(ArraySegment<byte> segment) => BitConverter.ToUInt32(segment.Array, segment.Offset);

        internal static int ReadInt32(ArraySegment<byte> segment) => BitConverter.ToInt32(segment.Array, segment.Offset);

        internal static ulong ReadUInt64(ArraySegment<byte> segment) => BitConverter.ToUInt64(segment.Array, segment.Offset);

        internal static long ReadInt64(ArraySegment<byte> segment) => BitConverter.ToInt64(segment.Array, segment.Offset);

        internal static float ReadSingle(ArraySegment<byte> segment) => BitConverter.ToSingle(segment.Array, segment.Offset);

        internal static string ReadUtf8String(ArraySegment<byte> segment)
        {
            var count = Array.IndexOf(segment.Array, (byte)0, segment.Offset, segment.Count) - segment.Offset;
            return System.Text.Encoding.UTF8.GetString(segment.Array, segment.Offset, count < 0 ? segment.Count : count);
        }

        internal static void Write(ArraySegment<byte> segment, byte value) => segment.Array[segment.Offset] = value;

        internal static void Write(ArraySegment<byte> segment, sbyte value) => segment.Array[segment.Offset] = (byte)value;

        internal static void Write(ArraySegment<byte> segment, ushort value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
        }

        internal static void Write(ArraySegment<byte> segment, short value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
        }

        internal static void Write(ArraySegment<byte> segment, uint value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
        }

        internal static void Write(ArraySegment<byte> segment, int value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
        }

        internal static void Write(ArraySegment<byte> segment, ulong value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
            segment.Array[segment.Offset + 4] = (byte)(value >> 32);
            segment.Array[segment.Offset + 5] = (byte)(value >> 40);
            segment.Array[segment.Offset + 6] = (byte)(value >> 48);
            segment.Array[segment.Offset + 7] = (byte)(value >> 56);
        }

        internal static void Write(ArraySegment<byte> segment, long value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
            segment.Array[segment.Offset + 4] = (byte)(value >> 32);
            segment.Array[segment.Offset + 5] = (byte)(value >> 40);
            segment.Array[segment.Offset + 6] = (byte)(value >> 48);
            segment.Array[segment.Offset + 7] = (byte)(value >> 56);
        }

        internal static unsafe void Write(ArraySegment<byte> segment, float value) => Write(segment, *(int*)&value);

        internal static unsafe void Write(ArraySegment<byte> segment, string value) =>
            System.Text.Encoding.UTF8.GetBytes(value, 0, Math.Min(value.Length, segment.Count), segment.Array, segment.Offset);

        internal static void Write<T>(ArraySegment<byte> segment, T[] values) where T : unmanaged
        {
            Buffer.BlockCopy(values, 0, segment.Array, segment.Offset, segment.Count);
        }

        internal static void Write<T>(ArraySegment<T> segment, T[] values)
        {
            Array.Copy(values, 0, segment.Array, segment.Offset, segment.Count);
        }
    }
}
