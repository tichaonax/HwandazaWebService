using System;
using System.Collections.Generic;
using System.Diagnostics;
using HwandazaWebService.Modules;
using SQLite.Net;

namespace HwandazaWebService.Utils
{
    internal class GpioProcessor
    {
        private readonly MainWaterPump _mainWaterPump;
        private readonly FishPondPump _fishPondPump;
        private readonly LawnIrrigator _lawnIrrigator;
        private readonly RandomLights _randomLights;
        private readonly SQLiteConnection _sqLiteConnection;

        public GpioProcessor(MainWaterPump mainWaterPump, FishPondPump fishPondPump, LawnIrrigator lawnIrrigator, RandomLights randomLights, SQLiteConnection sqLiteConnection)
        {
            _mainWaterPump = mainWaterPump;
            _fishPondPump = fishPondPump;
            _lawnIrrigator = lawnIrrigator;
            _randomLights = randomLights;
            _sqLiteConnection = sqLiteConnection;
        }

        public void ButtonWaterPump()
        {
            _mainWaterPump.ButtonPressed();
            UpdateHwandazaStatus();
        }

        public void ButtonFishPondPump()
        {
            _fishPondPump.ButtonPressed();
            UpdateHwandazaStatus();
        }

        public void ButtonLawnIrrigator()
        {
            _lawnIrrigator.ButtonPressed();
            UpdateHwandazaStatus();
        }

        private void CommandOff(HwandazaCommand command)
        {
            switch (command.Module.ToLower())
            {
                case Const.MainWaterPump:
                    _mainWaterPump.Stop();
                    break;
                case Const.FishPondPump:
                    _fishPondPump.Stop();
                    break;
                case Const.LawnIrrigator:
                    _lawnIrrigator.Stop();
                    break;
                case Const.RandomLights:
                    _randomLights.TurnOffLights(command.Lights);
                    break;
            }
        }

        private void CommandOn(HwandazaCommand command)
        {
            switch (command.Module.ToLower())
            {
                case Const.MainWaterPump:
                    _mainWaterPump.ManualOverideSwitch();
                    _mainWaterPump.Run();
                    break;
                case Const.FishPondPump:
                    _fishPondPump.ManualOverideSwitch();
                    _fishPondPump.Run();
                    break;
                case Const.LawnIrrigator:
                    _lawnIrrigator.ManualOverideSwitch();
                    _lawnIrrigator.Run();
                    break;
                case Const.RandomLights:
                    _randomLights.TurnOnLights(command.Lights);
                    break;
            }
        }

        private void CommandOperations(HwandazaCommand command)
        {
            throw new NotImplementedException();
        }

        private void ActOnCommand(HwandazaCommand command)
        {
            switch (command.Command.ToUpper())
            {
                case Const.CommandOn:
                    CommandOn(command);
                    break;

                case Const.CommandOff:
                    CommandOff(command);
                    break;
                case Const.CommandOperations:
                    CommandOperations(command);
                    break;
                case Const.CommandStatus:
                    break;
            }

            UpdateHwandazaStatus();
        }

        public void ProcessHwandazaCommand(HwandazaCommand command)
        {
            ActOnCommand(command);
            MarkOperationComplete(command.SqlRowGuidId);
        }

        private void MarkOperationComplete(string sqlRowGuidId)
        {
            var completed = _sqLiteConnection.Table<HwandazaCommandStatus>()
                .Where(x => x.RowGuidId == sqlRowGuidId).FirstOrDefault();
            if (completed != null)
            {
                _sqLiteConnection.Delete(completed);
            }

            Debug.WriteLine("GpioProcessor: Commnad Marked As Completed");
        }

        public void UpdateHwandazaStatus()
        {
            var lights = _randomLights.ModuleStatus().LightsStatus;
            var status = _sqLiteConnection.Table<HwandazaStatus>()
                .Where(x => x.RowGuidId == Const.StatusRowGuidId)
                .FirstOrDefault();

            var insert = (status == null);

            if (insert)
            {
                status = new HwandazaStatus {RowGuidId = Const.StatusRowGuidId };
            }

            var waterPump = _mainWaterPump.ModuleStatus();
            status.MainWaterPump = waterPump.IsRunning ? 1 : 0;
            status.MainWaterPumpAdcFloatValue = waterPump.AdcVoltage;

            var fishPond = _fishPondPump.ModuleStatus();
            status.FishPondPump = fishPond.IsRunning ? 1 : 0;
            status.FishPondPumpAdcFloatValue = fishPond.AdcVoltage;

            var lawnIrrigator = _lawnIrrigator.ModuleStatus();
            status.LawnIrrigator = lawnIrrigator.IsRunning ? 1 : 0;
            status.LawnIrrigatorAdcFloatValue = lawnIrrigator.AdcVoltage;

            status.M1 = lights.IsOnM1 ? 1 : 0;
            status.M2 = lights.IsOnM2 ? 1 : 0;
            status.L3 = lights.IsOnL3 ? 1 : 0;
            status.L4 = lights.IsOnL4 ? 1 : 0;
            status.L5 = lights.IsOnL5 ? 1 : 0;
            status.L6 = lights.IsOnL6 ? 1 : 0;
            status.SystemDate = DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss");

            if (insert)
            {
                _sqLiteConnection.Insert(status);
                Debug.WriteLine("GpioProcessor: New Record");
            }
            else
            {
                _sqLiteConnection.Update(status);
                Debug.WriteLine("GpioProcessor: Update Record");
            }
        }
        
        public void ButtonLights(List<string> lights)
        {
            _randomLights.ToggleLights(lights);
            UpdateHwandazaStatus();
        }
    }
}
