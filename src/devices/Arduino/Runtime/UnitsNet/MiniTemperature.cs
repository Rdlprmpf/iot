﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;

namespace Iot.Device.Arduino.Runtime.UnitsNet
{
    [ArduinoReplacement(typeof(Temperature), true)]
    internal struct MiniTemperature
    {
        private const double DEGREE_TO_KELVIN = 273.15;
        private double _value;
        private TemperatureUnit _unit;

        private MiniTemperature(double value, TemperatureUnit unit)
        {
            _value = value;
            _unit = unit;
        }

        public double DegreesCelsius
        {
            get
            {
                if (_unit == TemperatureUnit.DegreeCelsius)
                {
                    return _value;
                }
                else if (_unit == TemperatureUnit.Kelvin)
                {
                    return _value + DEGREE_TO_KELVIN;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public static Temperature FromDegreesCelsius(QuantityValue value)
        {
            double v = (double)value;
            return new Temperature(v, TemperatureUnit.DegreeCelsius);
        }
    }
}
