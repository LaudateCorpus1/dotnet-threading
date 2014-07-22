﻿// Copyright (c) Rackspace, US Inc. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace Rackspace.Threading
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for efficiently creating <see cref="Task"/> continuations,
    /// with automatic handling of faulted and canceled antecedent tasks.
    /// </summary>
    /// <threadsafety static="true" instance="false"/>
    /// <preliminary/>
    public static class CoreTaskExtensions
    {
        /// <summary>
        /// Synchronously execute a continuation when a task completes successfully.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled or faulted, the status of the antecedent is
        /// directly applied to the task returned by this method; it is not wrapped in an additional
        /// <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Then{TResult}(Task, Func{Task, Task{TResult}})"/> instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result returned from the <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Select<TResult>(this Task task, Func<Task, TResult> continuationFunction)
        {
            return task.Select(continuationFunction, false);
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status of the antecedent is directly applied to the task
        /// returned by this method; it is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Then{TResult}(Task, Func{Task, Task{TResult}}, bool)"/> instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result returned from the <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Select<TResult>(this Task task, Func<Task, TResult> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes successfully.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled or faulted, the status of the antecedent is
        /// directly applied to the task returned by this method; it is not wrapped in an additional
        /// <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use <see cref="Then{TSource, TResult}(Task{TSource}, Func{Task{TSource}, Task{TResult}})"/>
        /// instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result returned from the <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Select<TSource, TResult>(this Task<TSource> task, Func<Task<TSource>, TResult> continuationFunction)
        {
            return task.Select(continuationFunction, false);
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status of the antecedent is directly applied to the task
        /// returned by this method; it is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use <see cref="Then{TSource, TResult}(Task{TSource}, Func{Task{TSource}, Task{TResult}}, bool)"/>
        /// instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result returned from the <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Select<TSource, TResult>(this Task<TSource> task, Func<Task<TSource>, TResult> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes successfully.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled or faulted, the status of the antecedent is
        /// directly applied to the task returned by this method; it is not wrapped in an additional
        /// <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation actions. For non-trivial continuation actions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Then(Task, Func{Task, Task})"/> instead.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationAction">The continuation action to execute when <paramref name="task"/> completes successfully.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Select(this Task task, Action<Task> continuationAction)
        {
            return task.Select(continuationAction, false);
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status of the antecedent is directly applied to the task
        /// returned by this method; it is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation actions. For non-trivial continuation actions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Then(Task, Func{Task, Task}, bool)"/> instead.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationAction">The continuation action to execute when <paramref name="task"/> completes.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationAction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Select(this Task task, Action<Task> continuationAction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationAction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes successfully.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled or faulted, the status of the antecedent is
        /// directly applied to the task returned by this method; it is not wrapped in an additional
        /// <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation actions. For non-trivial continuation actions, use <see cref="Then{TSource}(Task{TSource}, Func{Task{TSource}, Task})"/>
        /// instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationAction">The continuation action to execute when <paramref name="task"/> completes successfully.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Select<TSource>(this Task<TSource> task, Action<Task<TSource>> continuationAction)
        {
            return task.Select(continuationAction, false);
        }

        /// <summary>
        /// Synchronously execute a continuation when a task completes. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent task is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status of the antecedent is directly applied to the task
        /// returned by this method; it is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation actions. For non-trivial continuation actions, use <see cref="Then{TSource}(Task{TSource}, Func{Task{TSource}, Task}, bool)"/>
        /// instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationAction">The continuation action to execute when <paramref name="task"/> completes.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationAction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Select<TSource>(this Task<TSource> task, Action<Task<TSource>> continuationAction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationAction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Synchronously execute a cleanup continuation when a task completes, regardless of the
        /// final <see cref="Task.Status"/> of the task. If the cleanup action completes
        /// successfully, the status of the antecedent is directly applied to the task returned by
        /// this method. Otherwise, the status of the faulted or canceled cleanup operation is
        /// directly applied to the task returned by this method.
        /// </summary>
        /// <remarks>
        /// <para>This method ensures that exception information provided by a faulted or canceled
        /// task is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Finally(Task, Func{Task, Task})"/> instead.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="cleanupAction">The cleanup continuation function to execute when <paramref name="task"/> completes.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="cleanupAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Finally(this Task task, Action<Task> cleanupAction)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (cleanupAction == null)
                throw new ArgumentNullException("cleanupAction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            task
                .ContinueWith(cleanupAction, TaskContinuationOptions.ExecuteSynchronously)
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                            completionSource.SetFromTask(task);
                        else
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            return completionSource.Task;
        }

        /// <summary>
        /// Synchronously execute a cleanup continuation when a task completes, regardless of the
        /// final <see cref="Task.Status"/> of the task. If the cleanup action completes
        /// successfully, the status of the antecedent is directly applied to the task returned by
        /// this method. Otherwise, the status of the faulted or canceled cleanup operation is
        /// directly applied to the task returned by this method.
        /// </summary>
        /// <remarks>
        /// <para>This method ensures that exception information provided by a faulted or canceled
        /// task is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the continuation is executed synchronously, this method should only be used for
        /// lightweight continuation functions. For non-trivial continuation functions, use a
        /// <see cref="Task"/> for the continuation operation and call
        /// <see cref="Finally(Task, Func{Task, Task})"/> instead.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="cleanupAction">The cleanup continuation function to execute when <paramref name="task"/> completes.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task
        /// completes successfully, the <see cref="Task{TResult}.Result"/> property will return
        /// the result of the antecedent <paramref name="task"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="cleanupAction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Finally<TResult>(this Task<TResult> task, Action<Task<TResult>> cleanupAction)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (cleanupAction == null)
                throw new ArgumentNullException("cleanupAction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            task
                .ContinueWith(cleanupAction, TaskContinuationOptions.ExecuteSynchronously)
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                            completionSource.SetFromTask(task);
                        else
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a cleanup continuation task when a task completes, regardless of the final
        /// <see cref="Task.Status"/> of the antecedent task. If the cleanup action completes
        /// successfully, the status of the antecedent is directly applied to the task returned by
        /// this method. Otherwise, the status of the faulted or canceled cleanup operation is
        /// directly applied to the task returned by this method.
        /// </summary>
        /// <remarks>
        /// <para>This method ensures that exception information provided by a faulted or canceled
        /// task is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="cleanupFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="cleanupFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="cleanupFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task"/> representing the asynchronous cleanup operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="cleanupFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Finally(this Task task, Func<Task, Task> cleanupFunction)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (cleanupFunction == null)
                throw new ArgumentNullException("cleanupFunction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            task
                .ContinueWith(cleanupFunction, TaskContinuationOptions.ExecuteSynchronously)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                            completionSource.SetFromTask(task);
                        else
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a cleanup continuation task when a task completes, regardless of the final
        /// <see cref="Task.Status"/> of the antecedent task. If the cleanup action completes
        /// successfully, the status of the antecedent is directly applied to the task returned by
        /// this method. Otherwise, the status of the faulted or canceled cleanup operation is
        /// directly applied to the task returned by this method.
        /// </summary>
        /// <remarks>
        /// <para>This method ensures that exception information provided by a faulted or canceled
        /// task is not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="cleanupFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="cleanupFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="cleanupFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task"/> representing the asynchronous cleanup operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task
        /// completes successfully, the <see cref="Task{TResult}.Result"/> property will return
        /// the result of the antecedent <paramref name="task"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="cleanupFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Finally<TResult>(this Task<TResult> task, Func<Task<TResult>, Task> cleanupFunction)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (cleanupFunction == null)
                throw new ArgumentNullException("cleanupFunction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            task
                .ContinueWith(cleanupFunction, TaskContinuationOptions.ExecuteSynchronously)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                            completionSource.SetFromTask(task);
                        else
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a continuation task when a task completes successfully. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled or faulted, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully. The continuation function returns a <see cref="Task{TResult}"/> which provides the final result of the continuation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result provided by the
        /// <see cref="Task{TResult}.Result"/> property of the task returned from <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Then<TResult>(this Task task, Func<Task, Task<TResult>> continuationFunction)
        {
            return task.Then(continuationFunction, false);
        }

        /// <summary>
        /// Execute a continuation task when a task completes. The continuation task is synchronously
        /// created by a continuation function, and then unwrapped to form the result of this method.
        /// The <paramref name="supportsErrors"/> parameter specifies whether the continuation is
        /// executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task{TResult}"/> which provides the final result of the continuation.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result provided by the
        /// <see cref="Task{TResult}.Result"/> property of the task returned from <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Then<TResult>(this Task task, Func<Task, Task<TResult>> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a continuation task when a task completes successfully. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled or faulted, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully. The continuation function returns a <see cref="Task{TResult}"/> which provides the final result of the continuation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result provided by the
        /// <see cref="Task{TResult}.Result"/> property of the task returned from <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Then<TSource, TResult>(this Task<TSource> task, Func<Task<TSource>, Task<TResult>> continuationFunction)
        {
            return task.Then(continuationFunction, false);
        }

        /// <summary>
        /// Execute a continuation task when a task completes. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <typeparam name="TResult">The type of the result produced by the continuation <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task{TResult}"/> which provides the final result of the continuation.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. When the task completes successfully,
        /// the <see cref="Task{TResult}.Result"/> property will contain the result provided by the
        /// <see cref="Task{TResult}.Result"/> property of the task returned from <paramref name="continuationFunction"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task<TResult> Then<TSource, TResult>(this Task<TSource> task, Func<Task<TSource>, Task<TResult>> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a continuation task when a task completes successfully. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled or faulted, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully. The continuation function returns a <see cref="Task"/> which provides the final result of the continuation.</param>
        /// <returns>A <see cref="Task"/> representing the unwrapped asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Then(this Task task, Func<Task, Task> continuationFunction)
        {
            return task.Then(continuationFunction, false);
        }

        /// <summary>
        /// Execute a continuation task when a task completes. The continuation task is synchronously
        /// created by a continuation function, and then unwrapped to form the result of this method.
        /// The <paramref name="supportsErrors"/> parameter specifies whether the continuation is
        /// executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task"/> which provides the final result of the continuation.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the unwrapped asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Then(this Task task, Func<Task, Task> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }

        /// <summary>
        /// Execute a continuation task when a task completes successfully. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled or faulted, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes successfully. The continuation function returns a <see cref="Task"/> which provides the final result of the continuation.</param>
        /// <returns>A <see cref="Task"/> representing the unwrapped asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Then<TSource>(this Task<TSource> task, Func<Task<TSource>, Task> continuationFunction)
        {
            return task.Then(continuationFunction, false);
        }

        /// <summary>
        /// Execute a continuation task when a task completes. The continuation
        /// task is synchronously created by a continuation function, and then unwrapped to
        /// form the result of this method. The <paramref name="supportsErrors"/>
        /// parameter specifies whether the continuation is executed if the antecedent task is faulted.
        /// </summary>
        /// <remarks>
        /// <para>If the antecedent <paramref name="task"/> is canceled, or faulted with <paramref name="supportsErrors"/>
        /// set to <see langword="false"/>, the status
        /// of the antecedent is directly applied to the task returned by this method; it is
        /// not wrapped in an additional <see cref="AggregateException"/>.
        /// </para>
        ///
        /// <note type="caller">
        /// Since the <paramref name="continuationFunction"/> is executed synchronously, this
        /// method should only be used for lightweight continuation functions. This restriction
        /// applies only to <paramref name="continuationFunction"/> itself, not to the
        /// <see cref="Task"/> returned by it.
        /// </note>
        /// </remarks>
        /// <typeparam name="TSource">The type of the result produced by the antecedent <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="task">The antecedent task.</param>
        /// <param name="continuationFunction">The continuation function to execute when <paramref name="task"/> completes. The continuation function returns a <see cref="Task"/> which provides the final result of the continuation.</param>
        /// <param name="supportsErrors"><see langword="true"/> if the <paramref name="continuationFunction"/> properly handles a faulted antecedent task; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="Task"/> representing the unwrapped asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="task"/> is <see langword="null"/>.
        /// <para>-or-</para>
        /// <para>If <paramref name="continuationFunction"/> is <see langword="null"/>.</para>
        /// </exception>
        public static Task Then<TSource>(this Task<TSource> task, Func<Task<TSource>, Task> continuationFunction, bool supportsErrors)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");

            TaskCompletionSource<VoidResult> completionSource = new TaskCompletionSource<VoidResult>();

            TaskContinuationOptions successContinuationOptions = supportsErrors ? TaskContinuationOptions.NotOnCanceled : TaskContinuationOptions.OnlyOnRanToCompletion;
            task
                .ContinueWith(continuationFunction, TaskContinuationOptions.ExecuteSynchronously | successContinuationOptions)
                .Unwrap()
                .ContinueWith(
                    t =>
                    {
                        if (task.Status == TaskStatus.RanToCompletion || supportsErrors && task.Status == TaskStatus.Faulted)
                            completionSource.SetFromTask(t);
                    }, TaskContinuationOptions.ExecuteSynchronously);

            TaskContinuationOptions failedContinuationOptions = supportsErrors ? TaskContinuationOptions.OnlyOnCanceled : TaskContinuationOptions.NotOnRanToCompletion;
            task
                .ContinueWith(t => completionSource.SetFromTask(t), TaskContinuationOptions.ExecuteSynchronously | failedContinuationOptions);

            return completionSource.Task;
        }
    }
}
