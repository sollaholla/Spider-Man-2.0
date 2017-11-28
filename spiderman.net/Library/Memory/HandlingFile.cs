using GTA;
using System;
using System.Diagnostics;
using static spiderman.net.Library.Memory.MemoryAccess;

namespace spiderman.net.Library.Memory
{
    /// <summary>
    /// Credits stillhere (LfxB) https://github.com/LfxB/DriveModes/
    /// </summary>
    public static class HandlingFile
    {
        /// <summary>
        /// The offset for vehicle handling.
        /// </summary>
        private static int _hOffset;

        /// <summary>
        /// Intitializes this class with the handling offset. 
        /// MUST be called for any of these methods to work.
        /// </summary>
        public unsafe static void Init()
        {
            var pattern = new Pattern("\x3C\x03\x0F\x85\x00\x00\x00\x00\x48\x8B\x41\x20\x48\x8B\x88", "xxxx????xxxxxxx");
            pattern.Init();
            var ptr = pattern.Get().ToInt64();
            _hOffset = *(int*)(ptr + 0x16);
        }

        /// <summary>
        /// Get a value from the vehicles handling data.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <param name="valueOffset">The offset of the value needed (example fMass = 0x000C)</param>
        /// <returns></returns>
        public unsafe static float GetHandlingValue(Vehicle vehicle, int valueOffset)
        {
            if (vehicle == null || !vehicle.Exists())
                return 0f;

            ulong vehPtr = (ulong)vehicle.MemoryAddress;
            ulong handlingPtr = *(ulong*)(vehPtr + (uint)_hOffset);
            float fValue = *(float*)(handlingPtr + (uint)valueOffset);
            return fValue;
        }

        //public unsafe static sbyte GetHandlingValueInt8(Vehicle vehicle, int exactOffset)
        //{
        //    ulong vehPtr = (ulong)vehicle.MemoryAddress;
        //    ulong handlingPtr = *(ulong*)(vehPtr + (uint)_hOffset);
        //    sbyte fValue = *(sbyte*)(handlingPtr + (uint)exactOffset);
        //    return fValue;
        //}

        public unsafe static void SetHandlingValue(Vehicle vehicle, int exactOffset, float currentValue)
        {
            Process[] processes = Process.GetProcessesByName("GTA5");
            if (processes.Length > 0)
            {
                using (CheatEngine.Memory memory = new CheatEngine.Memory(processes[0]))
                {
                    ulong vehPtr = (ulong)vehicle.MemoryAddress;
                    ulong handlingPtr = *(ulong*)(vehPtr + (uint)_hOffset);
                    IntPtr exactPointer = (IntPtr)(handlingPtr + (uint)exactOffset); //convert exact handling address to IntPtr

                    memory.WriteFloat(exactPointer, currentValue); //write
                }
            }
        }

        //public unsafe static void SetHandlingValueInt(Vehicle vehicle, int exactOffset, int currentValue)
        //{
        //    Process[] processes = Process.GetProcessesByName("GTA5");
        //    if (processes.Length > 0)
        //    {
        //        using (CheatEngine.Memory memory = new CheatEngine.Memory(processes[0]))
        //        {
        //            ulong vehPtr = (ulong)vehicle.MemoryAddress; //convert veh.MemoryAddress to ulong
        //            ulong handlingPtr = *(ulong*)(vehPtr + (uint)_hOffset); //add handling offset to address to get handling address
        //            IntPtr exactPointer = (IntPtr)(handlingPtr + (uint)exactOffset); //convert exact handling address to IntPtr

        //            memory.WriteInt32(exactPointer, currentValue); //write
        //        }
        //    }
        //}
    }
}
