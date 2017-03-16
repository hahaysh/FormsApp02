////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Gadgeteer.Modules
{
    using Gadgeteer;
    using Microsoft.SPOT;
    using GTI = Gadgeteer.Interfaces;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Threading;

    /// <summary>
    /// Abstract class to provide common methods, properties, and events for DaisyLink modules
    /// that can be chained together on the same socket.
    /// </summary>
    /// <remarks>
    /// The <see cref="DaisyLinkModule"/> class is the base class for all DaisyLink modules
    /// that are capable of being chained together on the same socket. When you use
    /// chained modules, you instantiate each corresponding object
    /// by providing the same socket to the object constructor.  
    /// For DaisyLink modules (using type X or Y), pin 3 is for the DaisyLink neighbor bus, pin 4 is 
    /// used for I2C SDA, pin 5 is used for I2C SCL. See the DaisyLink specification in Appendix 1 of the 
    /// Microsoft .NET Gadgeteer Module Builder�s Guide for more details.
    /// </remarks>
    public abstract class DaisyLinkModule : Module
    {
        /// <summary>
        /// Gets the position on the chain of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        public int PositionOnChain { get { return 1 + ModuleAddress - daisylink.StartAddress; } }

        /// <summary>
        /// Gets the number of modules on the chain of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        public int LengthOfChain { get { return daisylink.NodeCount; } }

        /// <summary>
        /// The mainboard socket number which this DaisyLink chain is plugged into.
        /// </summary>
        public int DaisyLinkSocketNumber { get { return daisylink.Socket.SocketNumber; } }

        /// <summary>
        /// Gets the address of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        protected readonly byte ModuleAddress;

        /// <summary>
        /// Gets the manufacturer code for this <see cref="DaisyLinkModule"/>.
        /// </summary>
        protected readonly byte Manufacturer;

        /// <summary>
        /// Gets the manufacturer-specific module type code of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        protected readonly byte ModuleType;

        /// <summary>
        /// Gets the module version of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        protected readonly byte ModuleVersion;

        /// <summary>
        /// Gets the daisy link version of this <see cref="DaisyLinkModule"/>.
        /// </summary>
        protected readonly byte DaisyLinkVersion;

        /// <summary>
        /// The <see cref="DaisyLink"/> object associated  with this <see cref="DaisyLinkModule"/>.
        /// </summary>
        private DaisyLink daisylink;

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that has this module plugged into it.</param>
        /// <param name="manufacturer">The manufacturer of the module.</param>
        /// <param name="moduleType">The manufacturer-specific type code of the module.</param>
        /// <param name="minModuleVersionSupported">The minimum acceptable firmware version for the module.</param>
        /// <param name="maxModuleVersionSupported">The maximum acceptable firmware version for the module.</param>
        /// <param name="clockRateKhz">The clock rate of the module.</param>
        /// <param name="moduleName">The module name.</param>
        /// <exception cref="T:System.Exception">
        /// <list type="bullet">
        ///   <item>The daisy link version of the module on the chain is an unsupported version.</item>
        ///   <item>The module type specified by <paramref name="moduleType"/> does not match the type found on the chain.</item>
        ///   <item>The firmware version is not supported; it is less than <paramref name="minModuleVersionSupported"/> or greater than <paramref name="maxModuleVersionSupported"/>.</item>
        /// </list>
        /// </exception>
        protected DaisyLinkModule(int socketNumber, byte manufacturer, byte moduleType, byte minModuleVersionSupported, byte maxModuleVersionSupported, int clockRateKhz, String moduleName)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            daisylink = DaisyLink.GetDaisyLinkForSocket(socket, this);
            if (daisylink.NodeCount == 0)
            {
                throw new Socket.InvalidSocketException("No DaisyLink modules are detected on socket " + socket +
                    ". Please check that the DaisyLink module " + this + " is plugged in to the correct socket.");
            }

            if (daisylink.ReservedCount >= daisylink.NodeCount)
            {
                throw new Socket.InvalidSocketException("Problem initializing " + this + ": attempting to initialize " + (daisylink.ReservedCount + 1) + " DaisyLink modules on socket " + socket + " but only " + daisylink.NodeCount + " modules were found.");
            }

            // NOTE:  PositionOnChain will be invalid until the end of the constructor (it is dependent on DeviceAddress) so daisylink.ReservedCount is used until then
            DaisyLinkVersion = daisylink.GetDaisyLinkVersion(daisylink.ReservedCount);
            if (DaisyLinkVersion != DaisyLinkVersionImplemented)
            {
                throw new ApplicationException("DaisyLink module " + this + " on Socket " + socket + " position " + (daisylink.ReservedCount + 1) + " uses unsupported DaisyLink version " + DaisyLinkVersion + " (expected " + DaisyLinkVersionImplemented + ")");
            }

            daisylink.GetModuleParameters(daisylink.ReservedCount, out Manufacturer, out ModuleType, out ModuleVersion);

            if (manufacturer != Manufacturer)
            {
                throw new ApplicationException("Problem initializing " + this + ": unexpected DaisyLink module on Socket " + socket + " position " + (daisylink.ReservedCount + 1) + ". (Has manufacturer code " + Manufacturer + ", but expected " + manufacturer + ")");
            }

            if (moduleType != ModuleType)
            {
                throw new ApplicationException("Problem initializing " + this + ": unexpected DaisyLink module on Socket " + socket + " position " + (daisylink.ReservedCount + 1) + ". (Has type " + ModuleType + ", but expected " + moduleType + ")");
            }

            if (ModuleVersion < minModuleVersionSupported || ModuleVersion > maxModuleVersionSupported)
            {
                throw new ApplicationException("Problem initializing " + this + ": daisyLink module " + moduleName + " on Socket " + socket + " position " + (daisylink.ReservedCount + 1) + " has unsupported PSoC firmware version of " + ModuleVersion + " (expected min " + minModuleVersionSupported + " max " + maxModuleVersionSupported + ")");
            }

            ModuleAddress = daisylink.ReserveNextDaisyLinkNodeAddress(this);
        }

        /// <summary>
        /// Gets the number of DaisyLink modules on the chain at the specified socket number. 
        /// This throws an exception if the socket number is invalid or if the socket does not support DaisyLink.  
        /// If the socket is valid but there are no DaisyLink modules on the chain, it does not throw an exception but instead returns zero.
        /// </summary>
        /// <param name="socketNumber">The socket number.</param>
        /// <returns>The number of DaisyLink modules attached to the chain from the specified socket number.</returns>
        protected static int GetLengthOfChain(int socketNumber)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, null, null);
            return DaisyLink.GetDaisyLinkForSocket(socket, null).NodeCount;
        }

        /// <summary>
        /// Gets the module type, module version number, and manufacturer for the DaisyLink module at a particular position on the chain.
        /// This throws an exception if the socket number is invalid or if the socket does not support DaisyLink.  
        /// </summary>
        /// <param name="socketNumber">The socket number with the DaisyLink chain of devices.</param>
        /// <param name="position">The position on the chain of the module to query (first module is at position one).</param>
        /// <param name="manufacturer">Out parameter that returns the module manufacturer.</param>
        /// <param name="type">Out parameter that returns the type of the module.</param>
        /// <param name="version">Out parameter that returns the module version number.</param>
        protected static void GetModuleParameters(int socketNumber, uint position, out byte manufacturer, out byte type, out byte version)
        {
            Socket socket = Socket.GetSocket(socketNumber, true, null, null);
            if (position >= 1) position--;
            DaisyLink.GetDaisyLinkForSocket(socket, null).GetModuleParameters(position, out manufacturer, out type, out version);
        }

        /// <summary>
        /// The number of DaisyLink reserved registers in the address space. 
        /// This is equivalent to the offset of the first register used by the module logic rather than DaisyLink.
        /// </summary>
        protected const byte DaisyLinkOffset = 8;

        /// <summary>
        /// The version number of the DaisyLink protocol implemented.
        /// </summary>
        protected const byte DaisyLinkVersionImplemented = 4;


        /// <summary>
        /// The delegate that is used for the <see cref="DaisyLinkInterrupt"/> event.
        /// </summary>
        /// <param name="sender">The DaisyLink module that raised the interrupt.</param>
        protected delegate void DaisyLinkInterruptEventHandler(DaisyLinkModule sender);

        /// <summary>
        /// Raised when a DaisyLink module raises an interrupt.
        /// </summary>
        protected event DaisyLinkInterruptEventHandler DaisyLinkInterrupt;
        private DaisyLinkInterruptEventHandler onDaisyLinkInterrupt;

        internal void OnDaisyLinkInterrupt(DaisyLinkModule sender)
        {
            DebugPrint("DaisyLink Module on socket " + sender.daisylink.Socket + " in position " + sender.PositionOnChain + " of " + sender.LengthOfChain + " has raised its interrupt.");
            if (onDaisyLinkInterrupt == null) onDaisyLinkInterrupt = new DaisyLinkInterruptEventHandler(OnDaisyLinkInterrupt);
            if (Program.CheckAndInvoke(DaisyLinkInterrupt, onDaisyLinkInterrupt, sender))
            {
                DaisyLinkInterrupt(sender);
            }

        }

        /// <summary>
        /// Reads a byte at the specified address from the DaisyLink module.
        /// </summary>
        /// <param name="memoryAddress">The address to read.</param>
        /// <returns>The byte at <paramref name="memoryAddress"/>.</returns>
        protected byte Read(byte memoryAddress)
        {
            return daisylink.ReadRegister(ModuleAddress, memoryAddress);
        }

        /// <summary>
        /// Writes the specified parameter bytes to the DaisyLink module.
        /// </summary>
        /// <param name="writeBuffer">The bytes to write.</param>
        /// <remarks>
        /// This method uses the <b>params</b> keyword in order
        /// to accept a variable number of bytes
        /// </remarks>
        protected void WriteParams(params byte[] writeBuffer)
        {
            daisylink.Write(ModuleAddress, writeBuffer);
        }

        /// <summary>
        /// Writes the specified bytes to the DaisyLink module.
        /// </summary>
        /// <param name="writeBuffer">The bytes to write</param>
        protected void Write(byte[] writeBuffer)
        {
            daisylink.Write(ModuleAddress, writeBuffer);
        }

        /// <summary>
        /// Writes an array of bytes to the module hardware and then reads an array of bytes from the DaisyLink module.
        /// </summary>
        /// <param name="writeBuffer">The array of bytes to write to the device.</param>
        /// <param name="writeOffset">The index of the first byte in the "writeBuffer" array to be written.</param>
        /// <param name="writeLength">The number of bytes from the "writeBuffer" array to be written.</param>
        /// <param name="readBuffer">The buffer that will hold the bytes read from the device.</param>
        /// <param name="readOffset">The index of the first byte that will be written to the "readBuffer" array.</param>
        /// <param name="readLength">The number of bytes that will be written to the "readBuffer" array.</param>
        /// <param name="numWritten">The number of bytes actually written to the device.</param>
        /// <param name="numRead">The number of bytes actually read from the device.</param>
        protected void WriteRead(byte[] writeBuffer, int writeOffset, int writeLength, byte[] readBuffer, int readOffset, int readLength, out int numWritten, out int numRead)
        {
            daisylink.WriteRead(ModuleAddress, writeBuffer, writeOffset, writeLength, readBuffer, readOffset, readLength, out numWritten, out numRead);
        }

        /// <summary>
        /// Represents the daisylink chain for a <see cref="DaisyLinkModule"/>.
        /// </summary>
        internal class DaisyLink : Gadgeteer.Interfaces.SoftwareI2C
        {
            // New DaisyLink pin numbers for type X sockets
            private const Socket.Pin daisyLinkPin = Socket.Pin.Three;
            private const Socket.Pin i2cDataPin = Socket.Pin.Four;
            private const Socket.Pin i2cClockPin = Socket.Pin.Five;
            private const byte defaultI2cAddress = 0x7F;

            protected static ArrayList daisyLinkList = new ArrayList();
            //private Cpu.Pin daisyLinkCpuPin;
            private GTI.DigitalIO daisyLinkResetPort;
            private GTI.InterruptInput daisyLinkInterruptPort;

            private static byte totalNodeCount = 0;
            private static object portLock = new object();
            private DaisyLinkModule[] socketModuleList;

            /// <summary>
            /// Provides an enumeration of registers that are used to query the link node.
            /// </summary>
            public enum DaisyLinkRegister : byte
            {
                /// <summary>
                /// The address of the node.
                /// </summary>
                Address = 0,
                /// <summary>
                /// The configuration byte:
                /// bit 0:  0 = pull-ups disabled, 1 = pull-ups enabled
                /// bit 1:  0 = function enabled,  1 = function disabled
                /// bit 7:  0 = not interrupting,  1 = interrupt condition
                /// </summary>
                Config = 1,
                /// <summary>
                /// The daisy link version of the node.
                /// </summary>
                DaisyLinkVersion = 2,
                /// <summary>
                /// The module type associated with the node.
                /// </summary>
                ModuleType = 3,
                /// <summary>
                /// The module version associated with the node.
                /// </summary>
                ModuleVersion = 4,
                /// <summary>
                /// The module manufacturer associated with this note.
                /// </summary>
                Manufacturer = 5,
            }

            /// <summary>
            /// Gets a value that indicates whether this <see cref="DaisyLink"/> is ready.
            /// </summary>
            public bool Ready { get; private set; }

            /// <summary>
            /// Gets the number of nodes associated with this link.
            /// </summary>
            public byte NodeCount { get; private set; }

            /// <summary>
            /// Gets the count of reserved nodes.
            /// </summary>
            public byte ReservedCount { get; private set; }

            /// <summary>
            /// Gets the starting address of this link.
            /// </summary>
            public byte StartAddress { get; private set; }

            /// <summary>
            /// The socket this DaisyLink chain is on.
            /// </summary>
            public Socket Socket
            {
                get;
                set;
            }


            /// <summary>
            /// Returns the DaisyLink instance for a given DaisyLink compatible socket.  
            /// If this is the first call to this method for a given socket, it creates a new DaisyLink instance, 
            /// which causes the chain to be initialised using the DaisyLink protocol.
            /// </summary>
            /// <param name="socket">The socket where the DaisyLink chain of modules is plugged in.</param>
            /// <param name="module">The daisylink module.</param>
            /// <returns>The DaisyLink instance for that socket</returns>
            public static DaisyLink GetDaisyLinkForSocket(Socket socket, DaisyLinkModule module)
            {
                lock (portLock)
                {
                    foreach (DaisyLink dl in daisyLinkList)
                    {
                        if (dl.Socket == socket)
                        {
                            return dl;
                        }
                    }

                    DaisyLink daisylink;
                    daisylink = new DaisyLink(socket, module);
                    daisyLinkList.Add(daisylink);
                    daisylink.Initialize();
                    return daisylink;
                }
            }

            /// <summary>
            /// Reserves the next node address on this DaisyLink chain.
            /// </summary>
            /// <returns>The I2C address of the next node on this DaisyLink chain.</returns>
            /// <exception cref="System.Exception">The chain is empty, or there is no more space on the chain for another node.</exception>
            internal byte ReserveNextDaisyLinkNodeAddress(DaisyLinkModule moduleInstance)
            {
                lock (portLock)
                {
                    if (ReservedCount >= this.NodeCount)
                    {
                        if (this.NodeCount == 0)
                        {
                            throw new ApplicationException("No DaisyLink modules are detected on socket " + Socket);
                        }
                        else
                        {
                            throw new ApplicationException("Attempting to initialize " + (ReservedCount + 1) + " modules on socket " + Socket + " but only " + NodeCount + " modules were found.");
                        }
                    }
                    this.socketModuleList[ReservedCount] = moduleInstance;
                    byte ret = (byte)(this.StartAddress + ReservedCount);
                    this.ReservedCount++;
                    return ret;
                }
            }

            private DaisyLink(Socket socket, DaisyLinkModule module)
                : base(socket, i2cDataPin, i2cClockPin, module)
            {
                Ready = false;
                this.Socket = socket;

                this.ReservedCount = 0;
                this.NodeCount = 0;

                // The link pin (port) is initialized as an input.  It is only driven during initialization of the daisylinked modules.
                // Setting the initial state to false insures that the pin will always drive low when Active is set to true.
                //daisyLinkCpuPin = socket.ReservePin(daisyLinkPin, module);
                daisyLinkResetPort = new GTI.DigitalIO(socket, daisyLinkPin, false, GTI.GlitchFilterMode.Off, GTI.ResistorMode.PullUp, module);
                daisyLinkInterruptPort = null;
            }

            /// <summary>
            /// Initializes the DaisyLink bus, resetting all devices on it and assigning them new addresses.  
            /// Any existing GTM.DaisyLinkModule devices will no longer work, and they should be constructed again.
            /// </summary>
            internal void Initialize()
            {
                lock (portLock)
                {
                    bool lastFound = false;
                    byte modulesFound = 0;

                    // Reset all modules in the chain and place the first module into Setup mode
                    SendResetPulse();

                    // For all modules in the chain
                    while (!lastFound)
                    {
                        if (DaisyLinkVersionImplemented != ReadRegister(defaultI2cAddress, (byte)DaisyLinkRegister.DaisyLinkVersion, LengthErrorBehavior.SuppressException))
                        {
                            lastFound = true;       // If the correct version can't be read back from a device, there are no more devices in the chain
                        }

                        if (modulesFound != 0)      // If a device is left in Standby mode
                        {
                            byte[] data = new byte[2] { (byte)DaisyLinkRegister.Config, (byte)(lastFound ? 1 : 0) };
                            Write((byte)(totalNodeCount + modulesFound), data);     // Enable/disable I2C pull-ups depending on whether last in chain (place module in Active mode)
                        }

                        if (!lastFound)
                        {
                            // Next module in chain is in Setup mode so start setting it up
                            modulesFound++;         // Increase the total number of modules found connected to this socket

                            byte[] data = new byte[2] { (byte)DaisyLinkRegister.Address, (byte)(totalNodeCount + modulesFound) };
                            Write(defaultI2cAddress, data);     // Set the I2C ID of the next module in the chain (place module in Standby mode)
                        }
                    }

                    this.StartAddress = (byte)(totalNodeCount + 1);
                    this.NodeCount = modulesFound;
                    this.ReservedCount = 0;
                    totalNodeCount += modulesFound;
                    Ready = true;
                    if (modulesFound != 0)
                    {
                        socketModuleList = new DaisyLinkModule[modulesFound];       // Keep track of all DaisyLinkModules attached to this socket
                        try
                        {
                            daisyLinkInterruptPort = new GTI.InterruptInput(Socket, daisyLinkPin, GTI.GlitchFilterMode.Off, GTI.ResistorMode.Disabled, GTI.InterruptMode.FallingEdge, null);
                        }

                        catch (Exception e)
                        {
                            throw new Socket.InvalidSocketException("There is an issue connecting the DaisyLink module to socket " + Socket +
                            ". Please check that all modules are connected to the correct sockets or try connecting the DaisyLink module to a different socket", e);
                        }
                        daisyLinkInterruptPort.Interrupt += daisyLinkInterruptPort_OnInterrupt;
                        //daisyLinkInterruptPort.EnableInterrupt();
                    }
                }
            }

            /// <summary>
            /// Sends a reset pulse on the daisylink chain.  This resets all DaisyLink nodes to INIT state, that is, waiting for a DaisyLink message.
            /// </summary>
            /// <remarks>
            /// It is recommended to reboot the mainboard after calling this method because communication to the DaisyLink nodes will fail.
            /// </remarks>
            internal void SendResetPulse()
            {
                lock (portLock)
                {
                    if (daisyLinkInterruptPort != null)
                    {
                        daisyLinkInterruptPort.Interrupt -= daisyLinkInterruptPort_OnInterrupt;
                        daisyLinkInterruptPort.Dispose();       // Ask hardware drivers to unreserve this pin
                        daisyLinkInterruptPort = null;
                    }
                    if (daisyLinkResetPort == null)
                    {
                        daisyLinkResetPort = new GTI.DigitalIO(Socket, daisyLinkPin, false, GTI.GlitchFilterMode.Off, GTI.ResistorMode.PullUp, null);
                    }
                    daisyLinkResetPort.IOMode = GTI.DigitalIO.IOModes.Output;  // Should drive the neighbor bus high
                    Thread.Sleep(2);                                           // 2 milliseconds is definitely more than 1 ms
                    daisyLinkResetPort.IOMode = GTI.DigitalIO.IOModes.Input;   // Pull-downs should take the neighbor bus back low
                    daisyLinkResetPort.Dispose();               // Remove this pin from the hardware's reserved pin list
                    daisyLinkResetPort = null;
                }
            }

            private void daisyLinkInterruptPort_OnInterrupt(GTI.InterruptInput sender, bool value)
            {
                for (byte moduleIndex = 0; moduleIndex < NodeCount; moduleIndex++)
                {
                    if (0 != (0x80 & ReadRegister((byte)(moduleIndex + StartAddress), (byte)DaisyLinkRegister.Config)))       // If this is the interrupting module
                    {
                        byte[] data = new byte[] { (byte)DaisyLinkRegister.Config, 0 };
                        Write((byte)(moduleIndex + StartAddress), data);                    // Clear the interrupt on the module
                        if (socketModuleList[moduleIndex] != null)
                        {
                            socketModuleList[moduleIndex].OnDaisyLinkInterrupt(socketModuleList[moduleIndex]);              // Kick off user event (if any) for this module instance
                        }
                    }
                }
                //daisyLinkInterruptPort.ClearInterrupt();
            }

            /// <summary>
            /// Gets the module type, module version number, and manufacturer for the module at a particular position on the chain.
            /// </summary>
            /// <param name="position">The position on the chain of the module to query (zero offset).</param>
            /// <param name="manufacturer">Out parameter that returns the module manufacturer.</param>
            /// <param name="type">Out parameter that returns the type of the module.</param>
            /// <param name="version">Out parameter that returns the module version number.</param>
            internal void GetModuleParameters(uint position, out byte manufacturer, out byte type, out byte version)
            {
                byte address;
                if (position >= NodeCount)
                {
                    throw new ApplicationException("Attempting to access module " + (position + 1) + " on socket " + Socket + " but only " + NodeCount + " modules were found.");
                }

                address = (byte)(StartAddress + position);
                type = ReadRegister(address, (byte)DaisyLinkRegister.ModuleType);
                version = ReadRegister(address, (byte)(DaisyLinkRegister.ModuleVersion));
                manufacturer = ReadRegister(address, (byte)(DaisyLinkRegister.Manufacturer));
            }

            /// <summary>
            /// Gets the version of the DaisyLink interface for the module at a particular position on the chain.
            /// </summary>
            /// <param name="position">The position on the chain of the module to query (zero offset).</param>
            /// <returns>The DaisyLink version number of the module.</returns>
            internal byte GetDaisyLinkVersion(uint position)
            {
                byte address;

                if (position >= NodeCount)
                {
                    throw new ApplicationException("Attempting to access module " + (position + 1) + " on socket " + Socket + " but only " + NodeCount + " modules were found.");
                }

                address = (byte)(StartAddress + position);
                return ReadRegister(address, (byte)DaisyLinkRegister.DaisyLinkVersion);
            }
        }
    }
}