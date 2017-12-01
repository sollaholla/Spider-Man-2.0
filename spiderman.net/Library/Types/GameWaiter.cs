using System;
using GTA;

namespace SpiderMan.Library.Types
{
    public static class GameWaiter
    {
        /// <summary>
        ///     Essentially the same as SHVDN Script.Wait except
        ///     this uses real game time (and frame rate) instead of system time.
        /// </summary>
        /// <param name="ms"></param>
        public static void Wait(int ms)
        {
            var sec = ms / 1000f;
            while (sec > 0)
            {
                sec -= Time.DeltaTime;
                Script.Yield();
            }
        }

        /// <summary>
        ///     Runs an action while the evaluation returns true.
        /// </summary>
        /// <param name="evaluation"></param>
        /// <param name="function"></param>
        public static void DoWhile(Func<bool> evaluation, Action function)
        {
            while (evaluation.Invoke())
            {
                function?.Invoke();
                Script.Yield();
            }
        }

        /// <summary>
        ///     Runs an action while the evaluation returns true, over the specified time in milliseconds.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="evaluation"></param>
        /// <param name="function"></param>
        public static void DoWhile(int timeout, Func<bool> evaluation, Action function)
        {
            var timer = timeout / 1000f;
            while (evaluation.Invoke() && timer > 0)
            {
                timer -= Time.DeltaTime;
                function?.Invoke();
                Script.Yield();
            }
        }

        /// <summary>
        ///     Yields the script until the evaluation returns true.
        /// </summary>
        /// <param name="evaluation"></param>
        public static void WaitUntil(int timeout, Func<bool> evaluation)
        {
            if (timeout != -1)
            {
                var timer = timeout / 1000f;
                while (!evaluation.Invoke() && timer > 0f)
                {
                    timer -= Time.DeltaTime;
                    Script.Yield();
                }
            }
            else
            {
                while (!evaluation.Invoke())
                    Script.Yield();
            }
        }
    }
}