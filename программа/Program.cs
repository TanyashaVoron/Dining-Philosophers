using System;
using System.Collections.Generic;
using System.Threading;

namespace philosophers_os
{
    class Control //контролируем статус лев, прав и текущего философов, меняем, по мере необходимости
    {
        public List<string> status = new List<string>(); //статус философа (ест/думает/ждет)

        Mutex m = new Mutex();

        HashSet<int> Nhungry = new HashSet<int>(); //номер философа в ожидании

        // N-номер философа

        public void newPhilosop()
        {
            status.Add("");
        }

        public bool LR(int N) //проверяем левого и правого философов
        {
            N--;
            m.WaitOne();

            var k = status.Count; 
            
            if (status[N] != "hungry" && (status[(N + k - 1) % k] == "hungry" || status[(N+1) % k] == "hungry"))
                status[N] = "waiting";

            if (status[N] != "waiting" && status[(N + k - 1) % k] != "eating" && status[(N+1) % k] != "eating")
            {
                status[N] = "eating";
                m.ReleaseMutex();
                return true;
            }
            m.ReleaseMutex();
            return false;
        }

        public void Hungry_(int N) //меняем состояние голодного
        {
            N--;
            m.WaitOne();

            if (status[N] != "waiting")
            {
                status[N] = "hungry";
                Nhungry.Add(++N);
            }
            m.ReleaseMutex();
        }

        public void Think_(int N) //меняем статус ожидающего
        {
            m.WaitOne();
            var k = status.Count;

            if (Nhungry.Contains(N))
            {
                status[(N + k - 2) % k] = "";
                status[N % k] = "";
                Nhungry.Remove(N);
            }

            status[N - 1] = "thinking";

            m.ReleaseMutex();
        }
    }

    class Fork
    {
        private Mutex m = new Mutex();

        public void take()
        {
            m.WaitOne();
        }

        public void put()
        {
            m.ReleaseMutex();
        }
    }

    class Philosopher
    {
        int id;
        Fork fork_left;
        Fork fork_right;
        uint eat_count;
        double wait_time;
        DateTime wait_start;
        bool stop_flag;
        bool debug_flag;
        Random random;

        uint Kgolod; //
        static Control m = new Control(); //

        void think()
        {
            if (this.debug_flag)
            {
                Console.WriteLine(this.id + " thinking");
            }

            Thread.Sleep(this.random.Next(0, 100));

            if (this.debug_flag)
            {
                Console.WriteLine(this.id + " hungry");
            }

            this.wait_start = DateTime.Now;
        }

        void eat()
        {
            this.wait_time += DateTime.Now.Subtract(this.wait_start).TotalMilliseconds;

            if (this.debug_flag)
            {
                Console.WriteLine(this.id + " eating");
            }

            Thread.Sleep(this.random.Next(0, 100));

            eat_count++;
        }

        public Philosopher(int number, Fork left, Fork right, bool dbg)
        {
            this.id = number;
            this.fork_left = left;
            this.fork_right = right;
            this.eat_count = 0;
            this.wait_time = 0;
            this.debug_flag = dbg;
            this.stop_flag = false;
            this.random = new Random();

            this.Kgolod = 0; //счетчик голода
            m.newPhilosop();
        }

        public void run()
        {
            while (!stop_flag)
            {
                think();

                while (!m.LR(id))// проверка соседей
                {
                    Kgolod++;
                    if (Kgolod > 9)
                        m.Hungry_(id); //меняем состояние голодного
                }

                Kgolod = 0;

                this.fork_left.take();
                if (this.debug_flag)
                {
                    Console.WriteLine(this.id + " took left fork");
                }

                this.fork_right.take();
                if (this.debug_flag)
                {
                    Console.WriteLine(this.id + " took right fork");
                }

                eat();

                this.fork_right.put();
                if (this.debug_flag)
                {
                    Console.WriteLine(this.id + " put right fork");
                }

                this.fork_left.put();
                if (this.debug_flag)
                {
                    Console.WriteLine(this.id + " put left fork");
                }

                m.Think_(id); //статус ожидающего
            }
        }

        public void stop()
        {
            stop_flag = true;
        }

        public void printStats()
        {
            Console.WriteLine(this.id + " " + this.eat_count + " " + Convert.ToInt32(this.wait_time));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            int N = 5;
            bool dbg = false;
            int duration = 60000;

            Fork[] forks = new Fork[N];
            for (int i = 0; i < N; i++)
            {
                forks[i] = new Fork();
            }

            Philosopher[] phils = new Philosopher[N];
            for (int i = 0; i < N; i++)
            {
                phils[i] = new Philosopher(i + 1, forks[(i + 1) % N], forks[i], dbg);
            }

            Thread[] runners = new Thread[N];
            for (int i = 0; i < N; i++)
            {
                runners[i] = new Thread(phils[i].run);
            }
            for (int i = 0; i < N; i++)
            {
                runners[i].Start();
            }

            Thread.Sleep(duration);

            for (int i = 0; i < N; i++)
            {
                phils[i].stop();
            }

            for (int i = 0; i < N; i++)
            {
                runners[i].Join();
            }

            for (int i = 0; i < N; i++)
            {
                phils[i].printStats();
            }

        }
    }
}