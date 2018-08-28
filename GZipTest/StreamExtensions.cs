using System;
using System.IO;

namespace GZipTest {

    /// <summary>
    /// РАсширения для работы с потоками
    /// </summary>
    public static class StreamExtensions {

        public static int ReadInt(this Stream stream, int default_value = default(int)) {
            var buf = new byte[4];
            return stream.Read(buf, 0, 4) == 0 ? default_value : buf.ToInt();
        }
        #region ----------------------- Сахар для IStreamExectContext ----------------------------
        private static void Exect(this IStreamExectContext context, long id, Action<long> action) {
            try {
                if (!context.OnWillStart(id))
                    return;
                action.Invoke(id);
            }
            catch (Exception e) {
                context.OnException(e);
            }
            finally {
                context.OnDidStart(id);
            }
        }

        private static bool TryComplite(this IStreamExectContext context, Func<BinaryBloc> action) {
            try {
                var bloc = action?.Invoke();

                if (context.OnComplite(bloc) || bloc == null)
                    return false;
            }
            catch (Exception e) {
                context?.OnException(e);
                return false;
            }

            return true;
        }
        #endregion
        #region ----------------------- Async Stream Reader ----------------------------
        /// <summary>
        /// Старт чтения потока
        /// </summary>
        /// <param name="stream">Источник</param>
        /// <param name="streamExectContext">контекст чтения</param>
        /// <param name="bufSize">Размер буфера чтения. Если не задан - то поблочное чтение, где размер блока это первые 4 байта</param>
        public static void BeginRead(this Stream stream, IStreamExectContext streamExectContext, int? bufSize = null) =>
            streamExectContext.Exect(streamExectContext.GetId(), id => {
                new StreamState {
                    Bloc = new BinaryBloc { Id = id, Buffer = new byte[bufSize ?? stream.ReadInt()] },
                    Stream = stream,
                    BufSize = bufSize,
                    Context = streamExectContext
                }.BeginRead(EndRead);
            });

        /// <summary>
        /// Завершение чтения
        /// </summary>
        private static void EndRead(IAsyncResult ar) {
            var state = ar?.AsyncState as StreamState;
            if (state.Context?.TryComplite(() => state.EndRead(ar)) ?? false)
                state.Stream.BeginRead(state.Context, state.BufSize);
        }

        #endregion

        #region ----------------------- Async Stream Writer ----------------------------

        /// <summary>
        /// Старт записи в поток
        /// </summary>
        /// <param name="stream">Поток</param>
        /// <param name="bloc">Записываемый блок</param>
        /// <param name="streamExectContext">Контекст записи</param>
        public static void BeginWrite(this Stream stream, BinaryBloc bloc, IStreamExectContext streamExectContext) =>
            streamExectContext.Exect(bloc.Id, id => {
                new StreamState {
                    Bloc = bloc,
                    Stream = stream,
                    BufSize = bloc.Buffer.Length,
                    Context = streamExectContext
                }.BeginWrite(EndWrite);
            });

        /// <summary>
        /// Завершение записи
        /// </summary>
        private static void EndWrite(IAsyncResult ar) {
            var state = ar?.AsyncState as StreamState;
            state.Context.TryComplite(() => state.EndWrite(ar));
        }

        #endregion
    }
}
