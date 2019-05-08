using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.Lib.Util
{
    public static class AsyncHelper
    {
        /// <summary>
        /// 将一个方法function异步运行，在执行完毕时执行回调callback.
        /// </summary>
        /// <param name="funcToRun——不带参数的Action"></param>
        /// <param name="callBack——不带参数的Action"></param>
        public static async void RunAsync(Action funcToRun, Action callBack)
        {
            Func<Task> taskFunc = () =>
            {
                return Task.Run(() =>
                {
                    funcToRun();
                });
            };
            await taskFunc();
            if (callBack != null)
                callBack();
        }

        public static async void Run2Async(Action funcToRun, Action callBack)
        {
            await Task.Run(() =>
            {
                funcToRun();
            });
            //await taskFunc();
            if (callBack != null)
                callBack();
        }

        public static async void RunAsync<TIn>(Action<TIn> funcToRun, TIn arg, Action callBack)
        {
            Func<Task> taskFunc = () =>
            {
                return Task.Run(() =>
                {
                    funcToRun(arg);
                });
            };
            await taskFunc();
            if (callBack != null)
                callBack();
        }
        /// <summary>
        /// webapi调用
        /// </summary>
        /// <typeparam name="TIn"></typeparam>
        /// <param name="funcToRun"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static async Task RunAsync<TIn>(Action<TIn> funcToRun, TIn arg)
        {
            Func<Task> taskFunc = () =>
            {
                return Task.Run(() =>
                {
                    funcToRun(arg);
                });
            };
            await taskFunc();
        }

        public static async void RunAsync<TIn,TIn2>(Action<TIn,TIn2> funcToRun, TIn arg,TIn2 arg2 ,Action callBack)
        {
            Func<Task> taskFunc = () =>
            {
                return Task.Run(() =>
                {
                    funcToRun(arg,arg2);
                });
            };
            await taskFunc();
            if (callBack != null)
                callBack();
        }

        public static async void RunAsync<TIn, TResult>(Func<TIn, TResult> funcToRun, TIn arg, Action<TResult> callBack)
        {
            Func<Task<TResult>> taskFunc = () =>
            {
                return Task<TResult>.Run<TResult>(() =>
                {
                    return funcToRun(arg);
                });
            };
            TResult t = await taskFunc();
            if (callBack != null)
                callBack(t);
        }


        public static async void RunAsync<TResult>(Func<TResult> funcToRun, Action<TResult> callBack)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task<TResult>.Run<TResult>(
                    () =>
                    {
                        return funcToRun();
                    }
                );
            };
            TResult r = await taskToRun();
            if (callBack != null)
                callBack(r);
        }

        /// <summary>
        /// 可以将同步函数变成异步调用
        /// </summary>
        /// <typeparam name="TIn1"></typeparam>
        /// <typeparam name="TIn2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="funcToRun"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public static async Task<TResult> RunAsync<TIn1,TIn2,TResult>(Func<TIn1,TIn2,TResult> funcToRun,TIn1 arg1,TIn2 arg2)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task.Run<TResult>(
                    () => funcToRun(arg1,arg2));
            };
            var r = await taskToRun();
            return r;
        }

        public static async Task<TResult> RunAsync<TResult>(Func<TResult> funcToRun)
        {
            Func<Task<TResult>> taskToRun = () => Task.Run<TResult>(
                funcToRun);
            var r = await taskToRun();
            return r;
        }

        public static async Task<TResult> RunAsync<TIn1, TResult>(Func<TIn1, TResult> funcToRun, TIn1 arg1)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task.Run<TResult>(
                    () => funcToRun(arg1));
            };
            var r = await taskToRun();
            return r;
        }

        public static async Task<TResult> RunAsync<TIn1, TIn2, TIn3, TResult>(
    Func<TIn1, TIn2, TIn3, TResult> funcToRun, TIn1 arg1, TIn2 arg2, TIn3 arg3)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task.Run<TResult>(
                    () => funcToRun(arg1, arg2, arg3));
            };
            var r = await taskToRun();
            return r;
        }

        public static async Task<TResult> RunAsync<TIn1, TIn2, TIn3, TIn4, TResult>(
            Func<TIn1, TIn2, TIn3, TIn4, TResult> funcToRun, TIn1 arg1, TIn2 arg2, TIn3 arg3, TIn4 arg4)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task.Run<TResult>(
                    () => funcToRun(arg1, arg2, arg3, arg4));
            };
            var r = await taskToRun();
            return r;
        }

        public static async Task<TResult> RunAsync<TIn1, TIn2, TIn3, TIn4, TIn5, TResult>(
            Func<TIn1, TIn2, TIn3, TIn4, TIn5, TResult> funcToRun, TIn1 arg1, TIn2 arg2, TIn3 arg3, TIn4 arg4, TIn5 arg5)
        {
            Func<Task<TResult>> taskToRun = () =>
            {
                return Task.Run<TResult>(
                    () => funcToRun(arg1, arg2, arg3, arg4, arg5));
            };
            var r = await taskToRun();
            return r;
        }
    }
}
