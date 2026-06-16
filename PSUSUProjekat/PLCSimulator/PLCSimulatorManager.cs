using System;
using System.Collections.Generic;
using System.Threading;

namespace PLCSimulator
{
    /// <summary>
    /// PLC Simulator
    /// 4 x ANALOG INPUT : ADDR001 - ADDR004
    /// 4 x ANALOG OUTPUT: ADDR005 - ADDR008
    /// 1 x DIGITAL INPUT: ADDR009, ADDR0011-ADDR0013
    /// 1 x DIGITAL OUTPUT: ADDR010, ADDR0014-ADDR0016
    /// </summary>
    public class PLCSimulatorManager
    {
        private Dictionary<string, double> addressValues;
        private object locker = new object();
        private Thread t1;
        private Thread t2;

        public PLCSimulatorManager()
        {
            addressValues = new Dictionary<string, double>();

            // AI
            addressValues.Add("ADDR001", 0);
            addressValues.Add("ADDR002", 0);
            addressValues.Add("ADDR003", 0);
            addressValues.Add("ADDR004", 0);

            // AO
            addressValues.Add("ADDR005", 0);
            addressValues.Add("ADDR006", 0);
            addressValues.Add("ADDR007", 0);
            addressValues.Add("ADDR008", 0);

            // DI
            addressValues.Add("ADDR009", 0);
            addressValues.Add("ADDR011", 0);
            addressValues.Add("ADDR012", 0);
            addressValues.Add("ADDR013", 0);

            // DO
            addressValues.Add("ADDR010", 0);
            addressValues.Add("ADDR014", 0);
            addressValues.Add("ADDR015", 0);
            addressValues.Add("ADDR016", 0);
        }

        public void StartPLCSimulator()
        {
            t1 = new Thread(GeneratingAnalogInputs);
            t1.IsBackground = true;
            t1.Start();

            t2 = new Thread(GeneratingDigitalInputs);
            t2.IsBackground = true;
            t2.Start();
        }

        private void GeneratingAnalogInputs()
        {
            while(true)
            {
                Thread.Sleep(100);
                lock(locker)
                {
                    addressValues["ADDR001"] = 100 * Math.Sin((double)DateTime.Now.Second / 60 * Math.PI);
                    addressValues["ADDR002"] = 100 * DateTime.Now.Second / 60;
                    addressValues["ADDR003"] = 50 * Math.Cos((double)DateTime.Now.Second / 60 * Math.PI);
                    addressValues["ADDR004"] = RandomNumberBetween(0, 50);
                }
            }
        }

        private void GeneratingDigitalInputs()
        {
            while(true)
            {
                Thread.Sleep(1000);
                lock(locker)
                {
                    addressValues["ADDR009"] = addressValues["ADDR009"] == 0 ? 1 : 0;
                    addressValues["ADDR011"] = addressValues["ADDR011"] == 0 ? 1 : 0;
                    addressValues["ADDR012"] = addressValues["ADDR012"] == 0 ? 1 : 0;
                    addressValues["ADDR013"] = addressValues["ADDR013"] == 0 ? 1 : 0;
                }
            }
        }

        public double GetValue(string address)
        {
            lock(locker)
            {
                if(addressValues.ContainsKey(address))
                    return addressValues[address];
                return -1;
            }
        }

        public void SetAnalogValue(string address, double value)
        {
            lock(locker)
            {
                if(addressValues.ContainsKey(address))
                    addressValues[address] = value;
            }
        }

        public void SetDigitalValue(string address, double value)
        {
            lock(locker)
            {
                if(addressValues.ContainsKey(address))
                    addressValues[address] = value;
            }
        }

        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            var random = new Random();
            return minValue + (random.NextDouble() * (maxValue - minValue));
        }

        public void Abort()
        {
            t1?.Abort();
            t2?.Abort();
        }
    }
}
