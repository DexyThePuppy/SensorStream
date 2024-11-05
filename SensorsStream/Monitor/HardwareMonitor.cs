using LibreHardwareMonitor.Hardware;
using System.IO;
using System.Linq;
using System;

namespace SensorStream.Monitor
{
    public class HardwareMonitor
    {
        private Computer computer;
        private JsonFormatter jsonFormatter;
        public HardwareMonitor(ComputerSettings computerSettings)
        {
            computer = new Computer
            {
                IsCpuEnabled = computerSettings.IsCpuEnabled,
                IsGpuEnabled = computerSettings.IsGpuEnabled,
                IsMemoryEnabled = computerSettings.IsMemoryEnabled,
                IsMotherboardEnabled = computerSettings.IsMotherboardEnabled,
                IsControllerEnabled = computerSettings.IsControllerEnabled,
                IsNetworkEnabled = computerSettings.IsNetworkEnabled,
                IsStorageEnabled = computerSettings.IsStorageEnabled
            };
            jsonFormatter = new JsonFormatter();
        }

        public void Open()
        {
            computer.Open();
        }

        public void Close()
        {
            computer.Close();
        }

        public string GetSystemData()
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();
                
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        string sensorKey = $"cpu/{sensor.SensorType.ToString().ToLower()}/{sensor.Name.ToLower()}";
                        jsonFormatter.FillSensors(sensorKey, sensor.Name, sensor.Value);
                    }
                }
                else if (hardware.HardwareType == HardwareType.GpuNvidia || 
                         hardware.HardwareType == HardwareType.GpuAmd || 
                         hardware.HardwareType == HardwareType.GpuIntel)
                {
                    // Handle iGPU data
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        jsonFormatter.FillSensors(sensor.SensorType.ToString(), sensor.Name, sensor.Value);
                    }
                }
                else if (hardware.HardwareType == HardwareType.Storage)
                {
                    // Extract capacity from drive name
                    float capacity = 0;
                    
                    if (hardware.Name.Contains("GB"))
                    {
                        // For NVMe drives that show capacity in name (e.g. "WD_BLACK SN850P for PS5 2000GB")
                        string capacityStr = hardware.Name.Split(' ').LastOrDefault(x => x.EndsWith("GB"));
                        if (float.TryParse(capacityStr?.Replace("GB", ""), out float parsedCapacity))
                        {
                            capacity = parsedCapacity;
                        }
                    }
                    else if (hardware.Name.Contains("TB"))
                    {
                        // For drives showing TB in name (e.g. "Samsung SSD 870 EVO 2TB")
                        string capacityStr = hardware.Name.Split(' ').LastOrDefault(x => x.EndsWith("TB"));
                        if (float.TryParse(capacityStr?.Replace("TB", ""), out float parsedCapacity))
                        {
                            capacity = parsedCapacity * 1024; // Convert TB to GB
                        }
                    }

                    if (capacity > 0)
                    {
                        jsonFormatter.FillSensors("Storage", "Total Capacity", capacity);
                    }

                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        jsonFormatter.FillSensors(sensor.SensorType.ToString(), sensor.Name, sensor.Value);
                    }
                }
                else
                {
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        jsonFormatter.FillSensors(sensor.SensorType.ToString(), sensor.Name, sensor.Value);
                    }
                }
                
                jsonFormatter.FillHardware(hardware.HardwareType.ToString(), hardware.Name);
            }
            return jsonFormatter.GetSerializedObject();
        }
    }
}

