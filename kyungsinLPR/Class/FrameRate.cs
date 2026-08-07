using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace KyungsinLPR
{
    /// <summary>
    /// Utility class to calculate the frame rate of camera.
    /// </summary>
    public class FrameRate
    {
        const int BUFFER_SIZE = 10;

        Stopwatch m_sw = new Stopwatch();
        Queue<long> m_queue = new Queue<long>();
        double m_frameRate = 0.0;

        public FrameRate()
        {
            m_sw.Start();
        }

        public void Grab()
        {
            try
            {
                if (m_queue.Count < BUFFER_SIZE)
                {
                    // buffer is not full.

                    long newval = m_sw.ElapsedMilliseconds;
                    m_queue.Enqueue(newval);
                    long val = m_queue.Peek();

                    if (m_queue.Count > 1 && (newval - val) != 0)
                        m_frameRate = (double)m_queue.Count * 1000 / (newval - val);
                    else
                        m_frameRate = 0.0; // can't calculate if there's only one value in the buffer.
                }
                else
                {
                    // buffer is full.

                    long newval = m_sw.ElapsedMilliseconds;
                    m_queue.Enqueue(newval);
                    long val = m_queue.Dequeue();

                    if (newval != val)
                        m_frameRate = (double)BUFFER_SIZE * 1000 / (newval - val);
                }
            }
            catch (Exception)
            { }
        }

        public double GetFrameRate()
        {
            return m_frameRate;
        }
    }
}
