using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest {

    /// <summary>
    /// Контекст обработки потока
    /// </summary>
    public interface IStreamExectContext {
        /// <summary>
        /// Полчить id блока, с которым работем
        /// </summary>
        /// <returns></returns>
        long GetId();
        /// <summary>
        /// Обработка блока завершена
        /// </summary>
        /// <param name="bloc">Обрабатываемый блок</param>
        /// <returns>Последний блок?</returns>
        bool OnComplite(BinaryBloc bloc);
        /// <summary>
        /// Обработчик пред-исполнения блока
        /// </summary>
        /// <param name="id">id блока</param>
        /// <returns>Продолжать исполнение?</returns>
        bool OnWillStart(long id);
        /// <summary>
        /// Обработчик пост-исполнения блока
        /// </summary>
        /// <param name="id">id блока</param>        
        void OnDidStart(long id);
        /// <summary>
        /// Ошибка выполнения
        /// </summary>
        /// <param name="e">Ошибка</param>
        void OnException(Exception e);
    }

    /// <summary>
    /// Класс синхронизации основоного потока, потока чтения и записи
    /// </summary>
    public class ReadWriteManager {
        /// <summary>
        /// Последний прочитанный блок
        /// </summary>
        private long _read = 0;
        /// <summary>
        /// Какой блок пишем
        /// </summary>
        private long _write = 1;
        /// <summary>
        /// Сколько потоков пишут
        /// </summary>
        private int _threadCount = 1;
        /// <summary>
        /// Событие синхронизации потоков записи
        /// </summary>
        private readonly EventWaitHandle _isWrite = new EventWaitHandle(true, EventResetMode.ManualReset);
        /// <summary>
        /// Событие синхронизации потоков чтения
        /// </summary>
        private readonly EventWaitHandle _isRead = new EventWaitHandle(true, EventResetMode.AutoReset);
        /// <summary>
        /// Событие синхронизации основного потока
        /// </summary>
        private readonly EventWaitHandle _isEnd = new EventWaitHandle(false, EventResetMode.ManualReset);
        /// <summary>
        /// Время создания 
        /// </summary>
        private readonly DateTime _start = DateTime.Now;

        /// <summary>
        /// Отладка
        /// </summary>
        public readonly bool Debug = false;

        public ReadWriteManager(bool debug = false) {
            Debug = debug;
        }

        /// <summary>
        /// Фабричный метод создания потоков обработки
        /// </summary>
        /// <param name="threadCount"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public IEnumerable<Thread> CreateThreadPool(int threadCount, ThreadStart action) {
            _threadCount = threadCount;
            return Enumerable.Range(0, threadCount).Select(i => new Thread(action));
        }
        
        /// <summary>
        /// Получить контекст чтения
        /// </summary>
        /// <param name="action">CallBack для прочитоного блока</param>
        /// <returns>Объект контекста чтения</returns>
        public IStreamExectContext GetReadExectContext(Action<BinaryBloc> onRead) => new ReadExectContext(this, onRead);
        /// <summary>
        /// Получить контекст записи
        /// </summary>
        /// <returns>Объект контекста записи</returns>
        public IStreamExectContext GetWriteExectContext() => new WriteExectContext(this);

        /// <summary>
        /// Начало чтения нового блока
        /// </summary>
        /// <returns>Id блока</returns>
        public long Read() {
            if (WaitHandle.WaitAny(new[] { _isRead, _isEnd }) == 1)
                return 0;
            
            return Interlocked.Increment(ref _read);
        }
        /// <summary>
        /// Завершение записи блока
        /// </summary>
        public void Write() => Interlocked.Increment(ref _write);


        public void IsLast() {
             if (_write == _read)
                _isWrite.Set();
        }

        /// <summary>
        /// Завершение потока исполнения
        /// </summary>
        public void ThreadEnd() {
            ConsoleWriteLine("Завершён");
            Interlocked.Decrement(ref _read);
            if (Interlocked.Decrement(ref _threadCount) == 0) {
                Interlocked.Increment(ref _read);
                for (int i = 0; i < 10000 && _isWrite.WaitOne(100); i++) IsLast();
                _isEnd.Set();
            }
        }
        /// <summary>
        /// Ожидаем завершения работы
        /// </summary>
        /// <returns>Завершон?</returns>
        public bool WaitOne() => _isEnd.WaitOne();

        /// <summary>
        /// Ожидаем завершения работы в течение timeMSec msSec
        /// </summary>
        /// <param name="timeMSec">Время ожидания завершения в msSec</param>
        /// <returns>Завершон?</returns>
        public bool WaitOne(int timeMSec) => _isEnd.WaitOne(timeMSec);

        /// <summary>
        /// Сколько прошло времени
        /// </summary>
        /// <returns>Сколько прошло времени</returns>
        public TimeSpan TotalTime() => DateTime.Now - _start;

        /// <summary>
        /// Ожидаем следующий блок на запись
        /// </summary>
        /// <param name="block">номер блока</param>
        /// <returns>Ещё не конец?</returns>
        public bool WaitWrite(long block) {
            while (_write != block && !_isEnd.WaitOne(10));
            _isWrite.Reset();
            return _isEnd.WaitOne(0);
        }
        /// <summary>
        /// Читаем следующий блок
        /// </summary>
        public void ReadOn() => _isRead.Set();

        /// <summary>
        /// Ошибки были?
        /// </summary>
        public void IsExceptionHappened() {
            if (_exception != null)
                throw _exception;
        }
        /// <summary>
        /// Ошибка
        /// </summary>
        private Exception _exception;

        /// <summary>
        /// Кажется что-то сломалось :(
        /// </summary>
        /// <param name="e">Ошибка</param>
        public void OnException(Exception e) {
            _exception = e;
            _isEnd.Set();
            _isWrite.Set();
        }

        public void ConsoleWriteLine(string str) {
            if (Debug) Console.WriteLine(string.Concat(Thread.CurrentThread.ManagedThreadId,": ",str));
        }
    }

    public class ReadExectContext : IStreamExectContext {

        private readonly ReadWriteManager _readWriteManager;
        private readonly Action<BinaryBloc> _onRead;

        public ReadExectContext(ReadWriteManager readWriteManager, Action<BinaryBloc> onRead) {
            _readWriteManager = readWriteManager;
            _onRead = onRead;
        }

        public long GetId() => _readWriteManager.Read();

        public bool OnComplite(BinaryBloc bloc) {
            if (bloc == null || bloc.Buffer == null || bloc.Buffer.Length == 0) {
                _readWriteManager.ThreadEnd();
                return true;
            }
            _readWriteManager.ConsoleWriteLine("чтение блока " + bloc.Id);
            _onRead?.Invoke(bloc);
            return false;
        }

        public void OnDidStart(long id) {
            _readWriteManager.ConsoleWriteLine("ожидает чтения блока " + id);
            _readWriteManager.ReadOn();
        }

        public void OnException(Exception e) => _readWriteManager.OnException(e);

        public bool OnWillStart(long id) => id > 0 && !_readWriteManager.WaitOne(0) ;
    }

    public class WriteExectContext : IStreamExectContext {
        private readonly ReadWriteManager _readWriteManager;

        public WriteExectContext(ReadWriteManager readWriteManager) {
            _readWriteManager = readWriteManager;
        }

        public long GetId() => throw new NotImplementedException();

        public bool OnComplite(BinaryBloc bloc) {
            if (bloc == null || bloc.Buffer == null || bloc.Buffer.Length == 0) {
                _readWriteManager.ThreadEnd();
                return true;
            }
            _readWriteManager.ConsoleWriteLine("запись блока " + bloc.Id);
            _readWriteManager.IsLast();
            return false;
        }

        public void OnDidStart(long id) => _readWriteManager.Write();

        public void OnException(Exception e) => _readWriteManager.OnException(e);

        public bool OnWillStart(long id) {
            _readWriteManager.ConsoleWriteLine("ожидает записи блока " + id);
            if (_readWriteManager.WaitWrite(id)) {
                _readWriteManager.ThreadEnd();
                return false;
            }
            return true;
        }
    }

}
