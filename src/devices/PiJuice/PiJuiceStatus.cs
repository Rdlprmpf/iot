﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using Iot.Device.PiJuiceDevice.Models;
using UnitsNet;

namespace Iot.Device.PiJuiceDevice
{
    /// <summary>
    /// PiJuiceStatus class to support status of the PiJuice
    /// </summary>
    public class PiJuiceStatus
    {
        private readonly PiJuice _piJuice;

        /// <summary>
        /// PiJuiceStatus constructor
        /// </summary>
        /// <param name="piJuice">The PiJuice class</param>
        public PiJuiceStatus(PiJuice piJuice)
        {
            _piJuice = piJuice;
        }

        /// <summary>
        /// Get basic PiJuice status information
        /// </summary>
        /// <returns>PiJuice status</returns>
        public Status GetStatus()
        {
            byte[] response = _piJuice.ReadCommand(PiJuiceCommand.Status, 1);
            return new Status(
                (response[0] & 0x01) == 0x01,
                (response[0] & 0x02) == 0x02,
                (BatteryState)(response[0] >> 2 & 0x03),
                (PowerInState)(response[0] >> 4 & 0x03),
                (PowerInState)(response[0] >> 6 & 0x03));
        }

        /// <summary>
        /// Get current fault status of PiJuice
        /// </summary>
        /// <returns>PiJuice fault status</returns>
        public FaultStatus GetFaultStatus()
        {
            byte[] response = _piJuice.ReadCommand(PiJuiceCommand.FaultEvent, 1);
            return new FaultStatus(
                (response[0] & 0x01) == 0x01,
                (response[0] & 0x02) == 0x02,
                (response[0] & 0x04) == 0x04,
                (response[0] & 0x08) == 0x08,
                (response[0] & 0x20) == 0x20,
                (BatteryChargingTemperatureFault)(response[0] >> 6 & 0x03));
        }

        /// <summary>
        /// Gets event generated by PiJuice buttons presses
        /// </summary>
        /// <returns>List of button event types</returns>
        public List<ButtonEventType> GetButtonEvents()
        {
            byte[] response = _piJuice.ReadCommand(PiJuiceCommand.ButtonEvent, 2);
            return new List<ButtonEventType>(3)
            {
                Enum.IsDefined(typeof(ButtonEventType), response[0] & 0x0F) ? (ButtonEventType)(response[0] & 0x0F) : ButtonEventType.Unknown,
                Enum.IsDefined(typeof(ButtonEventType), (response[0] >> 4) & 0x0F) ? (ButtonEventType)((response[0] >> 4) & 0x0F) : ButtonEventType.Unknown,
                Enum.IsDefined(typeof(ButtonEventType), response[1] & 0x0F) ? (ButtonEventType)(response[1] & 0x0F) : ButtonEventType.Unknown,
            };
        }

        /// <summary>
        /// Clears generated button event
        /// </summary>
        /// <param name="button">Button to clear button event for</param>
        public void ClearButtonEvent(ButtonSwitch button)
        {
            byte[] array = button switch
            {
                ButtonSwitch.Switch1 => new byte[] { 0xF0, 0xFF },
                ButtonSwitch.Switch2 => new byte[] { 0x0F, 0xFF },
                ButtonSwitch.Switch3 => new byte[] { 0xFF, 0xF0 },
                _ => throw new NotImplementedException()
            };

            _piJuice.WriteCommand(PiJuiceCommand.ButtonEvent, array);
        }

        /// <summary>
        /// Get battery charge level between 0 and 100 percent
        /// </summary>
        /// <returns>Battery charge level percentage</returns>
        public byte GetChargeLevel()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.ChargeLevel, 1);

            return response[0];
        }

        /// <summary>
        /// Get battery temperature
        /// </summary>
        /// <returns>Battery temperature in celsius</returns>
        public Temperature GetBatteryTemperature()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.BatteryTemperature, 2);

            return Temperature.FromDegreesCelsius(BinaryPrimitives.ReadInt16LittleEndian(response));
        }

        /// <summary>
        /// Get battery voltage
        /// </summary>
        /// <returns>Battery voltage in millivolts</returns>
        public ElectricPotential GetBatteryVoltage()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.BatteryVoltage, 2);

            return ElectricPotential.FromMillivolts(BinaryPrimitives.ReadInt16LittleEndian(response));
        }

        /// <summary>
        /// Get battery current
        /// </summary>
        /// <returns>Battery current in milliamps</returns>
        public ElectricCurrent GetBatteryCurrent()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.BatteryCurrent, 2);

            return ElectricCurrent.FromMilliamperes(BinaryPrimitives.ReadInt16LittleEndian(response));
        }

        /// <summary>
        /// Get supplied voltage
        /// </summary>
        /// <returns>Voltage supplied from the GPIO power output from the PiJuice or when charging, voltage supplied in millivolts</returns>
        public ElectricPotential GetIOVoltage()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.IOVoltage, 2);

            return ElectricPotential.FromMillivolts(BinaryPrimitives.ReadInt16LittleEndian(response));
        }

        /// <summary>
        /// Get supplied current in milliamps
        /// </summary>
        /// <returns>Current supplied from the GPIO power output from the PiJuice or when charging, current supplied in milliamps</returns>
        public ElectricCurrent GetIOCurrent()
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.IOCurrent, 2);

            return ElectricCurrent.FromMilliamperes(BinaryPrimitives.ReadInt16LittleEndian(response));
        }

        /// <summary>
        /// Get the color for a specific Led
        /// </summary>
        /// <param name="led">Led to get color for</param>
        /// <returns>Color of Led</returns>
        public Color GetLedState(Led led)
        {
            var response = _piJuice.ReadCommand(PiJuiceCommand.LedState + (byte)led, 3);

            return Color.FromArgb(0, response[0], response[1], response[2]);
        }

        /// <summary>
        /// Set the color for a specific Led
        /// </summary>
        /// <param name="led">Led to which color is to be applied</param>
        /// <param name="color">Color for the Led</param>
        public void SetLedState(Led led, Color color)
        {
            _piJuice.WriteCommand(PiJuiceCommand.LedState + (byte)led, new byte[] { color.R, color.G, color.B });
        }

        /// <summary>
        /// Get blinking pattern for a specific Led
        /// </summary>
        /// <param name="led">Led to get blinking pattern for</param>
        /// <returns>Led blinking pattern</returns>
        public LedBlink GetLedBlink(Led led)
        {
            byte[] response = _piJuice.ReadCommand(PiJuiceCommand.LedBlink + (byte)led, 9);

            return new LedBlink(
                led,
                response[0] == 255,
                response[0],
                Color.FromArgb(0, response[1], response[2], response[3]),
                new TimeSpan(0, 0, 0, 0, response[4] * 10),
                Color.FromArgb(0, response[5], response[6], response[7]),
                new TimeSpan(0, 0, 0, 0, response[8] * 10));
        }

        /// <summary>
        /// Set blinking pattern for a specific Led
        /// </summary>
        /// <param name="ledBlink">Led blinking pattern</param>
        public void SetLedBlink(LedBlink ledBlink)
        {
            if (ledBlink.Count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(ledBlink.Count));
            }

            if (ledBlink.FirstPeriod.TotalMilliseconds is < 10 or > 2550)
            {
                throw new ArgumentOutOfRangeException(nameof(ledBlink.FirstPeriod));
            }

            if (ledBlink.SecondPeriod.TotalMilliseconds is < 10 or > 2550)
            {
                throw new ArgumentOutOfRangeException(nameof(ledBlink.SecondPeriod));
            }

            var data = new byte[9];

            data[0] = (byte)(ledBlink.Count & 0xFF);
            data[1] = ledBlink.ColorFirstPeriod.R;
            data[2] = ledBlink.ColorFirstPeriod.G;
            data[3] = ledBlink.ColorFirstPeriod.B;
            data[4] = (byte)((int)(ledBlink.FirstPeriod.TotalMilliseconds / 10) & 0xFF);
            data[5] = ledBlink.ColorSecondPeriod.R;
            data[6] = ledBlink.ColorSecondPeriod.G;
            data[7] = ledBlink.ColorSecondPeriod.B;
            data[8] = (byte)((int)(ledBlink.SecondPeriod.TotalMilliseconds / 10) & 0xFF);

            _piJuice.WriteCommand(PiJuiceCommand.LedBlink + (byte)ledBlink.Led, data);
        }
    }
}
