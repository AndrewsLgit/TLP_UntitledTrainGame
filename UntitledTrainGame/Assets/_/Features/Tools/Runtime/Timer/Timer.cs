using System;

namespace Tools.Runtime
{
    public abstract class Timer {
        protected float initialTime;
        protected float Time { get; set; }
        public bool IsRunning { get; protected set; }
        
        public float Progress => Time / initialTime;
        
        public Action OnTimerStart = delegate { };
        public Action OnTimerStop = delegate { };
        public Action<float> OnTimerTick = delegate { };

        protected Timer(float value) {
            initialTime = value;
            IsRunning = false;
        }

        public void Start() {
            Time = initialTime;
            if (!IsRunning) {
                IsRunning = true;
                OnTimerStart.Invoke();
                OnTimerTick.Invoke(Progress);
            }
        }

        public void Stop() {
            if (IsRunning) {
                IsRunning = false;
                OnTimerStop.Invoke();
            }
        }
        
        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;
        
        public abstract void Tick(float deltaTime);
        
        public float GetTime() => Time;
        public float GetProgress() => Progress;
    }
    
    public class CountdownTimer : Timer {
        public CountdownTimer(float value) : base(value) { }

        public override void Tick(float deltaTime) {
            if (IsRunning && Time > 0) {
                float previousProgress = Progress;
                Time -= deltaTime;
                
                if(Math.Abs(Progress - previousProgress) > 0.001f) OnTimerTick.Invoke(Progress);
            }
            
            if (IsRunning && Time <= 0) {
                OnTimerTick.Invoke(Progress);
                Stop();
            }
        }
        
        public bool IsFinished => Time <= 0;
        
        public void Reset() => Time = initialTime;
        
        public void Reset(float newTime) {
            initialTime = newTime;
            Reset();
        }
    }
    
    public class StopwatchTimer : Timer {
        public StopwatchTimer() : base(0) { }

        public override void Tick(float deltaTime) {
            if (IsRunning) {
                float previousProgress = Progress;
                Time += deltaTime;
                
                if(Math.Abs(Progress - previousProgress) > 0.001f) OnTimerTick.Invoke(Progress);
            }
        }
        
        public void Reset() => Time = 0;
        
        public float GetTime() => Time;
    }
}