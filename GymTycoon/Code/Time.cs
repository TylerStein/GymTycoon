using ImGuiNET;
using System;
using System.Linq;

namespace GymTycoon.Code
{
    public class Time
    {
        public float MinuteRealTimeSeconds = 1f;

        private int _timeScaleIndex = 1;
        private readonly float[] _timeScales = [0f, 1f, 2f, 4f, 8f, 16f, 32f];

        public bool Metric = true;

        public bool DidChangeMinute = false;
        public bool DidChangeHour = false;
        public bool DidChangeDay = false;
        public bool DidChangeWeek = false;
        public bool DidChangeMonth = false;
        public bool DidChangeYear = false;

        private DateOnly _dateTime;
        private int _hour = 0;
        private int _minute = 0;

        private float _minuteTimer = 0f;

        public override string ToString()
        {
            return $"{_dateTime.ToShortDateString()} {_hour}:{MinuteToString()}";
        }

        public string MinuteToString()
        {
            return _minute > 9 ? _minute.ToString() : $"0{_minute}";
        }

        public void SetDate(int year, int month = 0, int day = 0, int hour = 0, int minute = 0)
        {
            _dateTime = new DateOnly(year, month, day);
            _hour = hour;
            _minute = minute;
        }

        public void SetDate(DateOnly date, int hour = 0)
        {
            _dateTime = date;
            _hour = hour;
        }

        public DateOnly GetCurrentDate()
        {
            return _dateTime;
        }

        public DateOnly GetPreviousDate()
        {
            return _dateTime.AddDays(-1);
        }

        public DateTime GetDateTime()
        {
            return new DateTime(_dateTime.Year, _dateTime.Month, _dateTime.Day, _hour, _minute, 0, 0, 0);
        }

        public int GetHour()
        {
            return _hour;
        }

        public int GetMinute()
        {
            return _minute;
        }

        public void Update(float deltaTime)
        {
            DidChangeMinute = false;
            DidChangeHour = false;
            DidChangeDay = false;
            DidChangeWeek = false;
            DidChangeMonth = false;
            DidChangeYear = false;

            _minuteTimer += deltaTime;
            if (_minuteTimer >= MinuteRealTimeSeconds)
            {
                _minuteTimer = 0f;
                _minute++;
                DidChangeMinute = true;

                if (_minute > 59)
                {
                    _minute = 0;
                    _hour++;
                    DidChangeHour = true;

                    if (_hour > 23)
                    {
                        _hour = 0;

                        DateOnly last = _dateTime;
                        _dateTime = _dateTime.AddDays(1);
                        DidChangeDay = true;

                        // Monday (0) thru Sunday (6)
                        if (_dateTime.DayOfWeek == 0)
                        {
                            DidChangeWeek = true;
                        }

                        if (_dateTime.Month != last.Month)
                        {
                            DidChangeMonth = true;
                        }

                        if (_dateTime.Year != last.Year)
                        {
                            DidChangeYear = true;
                        }
                    }
                }
            }
        }

        public float GetTimeScale()
        {
            return _timeScales[_timeScaleIndex];
        }

        public void PauseTimeScale()
        {
            _timeScaleIndex = 0;
        }

        public void DrawImGui()
        {
            ImGui.Begin("Time");

            ImGui.Text($"{_dateTime.ToShortDateString()} {_hour}:{MinuteToString()}");


            if (ImGui.ArrowButton("TimeScaleDecrement", ImGuiDir.Left))
            {
                if (_timeScaleIndex > 0)
                {
                    _timeScaleIndex--;
                }
            }
            ImGui.SameLine();
            ImGui.Text($"{GetTimeScale()}x");
            ImGui.SameLine();
            if (ImGui.ArrowButton("TimeScaleIncrement", ImGuiDir.Right))
            {
                if (_timeScaleIndex < _timeScales.Count() - 1)
                {
                    _timeScaleIndex++;
                }
            }

            if (ImGui.CollapsingHeader("Debug"))
            {
                if (ImGui.Button("End of Hour"))
                {
                    _minute = 59;
                    _minuteTimer = MinuteRealTimeSeconds;
                }

                if (ImGui.Button("End of Day"))
                {
                    _hour = 23;
                    _minuteTimer = MinuteRealTimeSeconds;
                }

                if (ImGui.Button("End of Week"))
                {
                    int dayOfWeek = (int)_dateTime.DayOfWeek;
                    DateOnly nextDate = _dateTime.AddDays(7 - dayOfWeek);
                    _hour = 23;
                    _minuteTimer = MinuteRealTimeSeconds;
                    _dateTime = nextDate;
                }

                if (ImGui.Button("End of Month"))
                {
                    DateOnly nextDate = new DateOnly(_dateTime.Year, _dateTime.Month + 1, 1);
                    _dateTime = nextDate.AddDays(-1);
                    _hour = 23;
                    _minuteTimer = MinuteRealTimeSeconds;
                }

                if (ImGui.Button("End of Year"))
                {
                    DateOnly nextDate = new DateOnly(_dateTime.Year + 1, 1, 1);
                    _dateTime = nextDate.AddDays(-1);
                    _hour = 23;
                    _minuteTimer = MinuteRealTimeSeconds;
                }
            }

            ImGui.End();
        }
    }
}
