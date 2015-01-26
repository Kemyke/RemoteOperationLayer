using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace ArdinDIContainer
{
    public partial class DIContainer
    {
        private class RunOnTypeRegisteredOrAvailableQueue
        {
            private Action<bool, string, Action> itemExecuterAction = null;

            private IDIContainer diContainer = null;
            private SpinLock syncRoot = new SpinLock();
            private Queue<QueueItem> queue = new Queue<QueueItem>();

            /// <summary>
            /// Queue implementation.
            /// </summary>
            /// <param name="diContainer"></param>
            /// <param name="itemExecuterAction">itemExecuterAction(isRunningSynchronouslyRequested, itemName, itemAction)</param>
            public RunOnTypeRegisteredOrAvailableQueue(IDIContainer diContainer, Action<bool, string, Action> itemExecuterAction)
            {
                if (diContainer == null)
                {
                    throw new ArgumentNullException("diContainer");
                }
                if (itemExecuterAction == null)
                {
                    throw new ArgumentNullException("itemExecuterAction");
                }
                this.diContainer = diContainer;
                this.itemExecuterAction = itemExecuterAction;

                this.diContainer.NewTypesRegistered += new EventHandler(diContainer_NewTypesRegistered);
            }

            private void diContainer_NewTypesRegistered(object sender, EventArgs e)
            {
                RunQueue();
            }


            public void AddWorkItem(bool isAvailableRequested, List<Type> dependsOnTypeList, Action action, bool isAsyncRunForced)
            {
                if (dependsOnTypeList == null)
                {
                    throw new ArgumentNullException("interfaceTypeList");
                }
                if (action == null)
                {
                    throw new ArgumentNullException("action");
                }

                var item = new QueueItem(isAvailableRequested, dependsOnTypeList, action);

                if (!isAsyncRunForced)
                {
                    var isExecutedSynchronously = RunItemIfRunnable(true, item);
                    if (!isExecutedSynchronously)
                    {
                        EnqueueItem(item);

                        System.Diagnostics.Debug.WriteLine("Method {0} waiting for items {1} become {2}", item.ToString(), GetWaitedItemNameList(item), (isAvailableRequested) ? "available" : "registered");

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Prerequisites of method {0} were {1}, method completed synchronously", item.ToString(), (isAvailableRequested) ? "available" : "registered");
                    }
                }
                else
                {
                    EnqueueItem(item);
                    RunQueue();
                }
            }

            private void EnqueueItem(QueueItem item)
            {
                bool lockTaken = false;
                try
                {
                    syncRoot.Enter(ref lockTaken);

                    // enqueue
                    queue.Enqueue(item);

                }
                finally
                {
                    if (lockTaken)
                    {
                        syncRoot.Exit();
                    }
                }
            }


            private int runQueueInProgress = 0;
            private bool runQueueReenteredDuringProgress = false;
            private void RunQueue()
            {

                if (Interlocked.CompareExchange(ref runQueueInProgress, 1, 0) == 0)
                {
                    try
                    {
                        QueueItem firstItemLeftInQueue = null;

                        while (true)
                        {
                            QueueItem queueItemToProcess = null;

                            bool lockTaken = false;
                            try
                            {
                                syncRoot.Enter(ref lockTaken);

                                if (queue.Count > 0)
                                {
                                    queueItemToProcess = queue.Peek();
                                    if (queueItemToProcess == firstItemLeftInQueue)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        queue.Dequeue();
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            finally
                            {
                                if (lockTaken)
                                {
                                    syncRoot.Exit();
                                }
                            }


                            if (!RunItemIfRunnable(false, queueItemToProcess))
                            {
                                EnqueueItem(queueItemToProcess);

                                System.Diagnostics.Debug.WriteLine("Method {0} still waiting for items {1} become {2}", queueItemToProcess.ToString(), GetWaitedItemNameList(queueItemToProcess), (queueItemToProcess.IsAvailable) ? "available" : "registered");

                                if (firstItemLeftInQueue == null)
                                {
                                    firstItemLeftInQueue = queueItemToProcess;
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Prerequisites of method {0} become {1}, method completed asynchronously", queueItemToProcess.ToString(), (queueItemToProcess.IsAvailable) ? "available" : "registered");
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.CompareExchange(ref runQueueInProgress, 0, 1);
                    }

                    if (runQueueReenteredDuringProgress)
                    {
                        // reentrance occured, possible new items enqueued; rerun queue
                        runQueueReenteredDuringProgress = false;
                        RunQueue();
                    }
                }
                else
                {
                    runQueueReenteredDuringProgress = true;
                }
            }

            private string GetWaitedItemNameList(QueueItem item)
            {
                string ret = null;

                var l = new List<string>();
                foreach (var t in item.DependsOnTypeList)
                {
                    if (item.IsAvailable)
                    {
                        if (!diContainer.IsAvailable(t))
                        {
                            l.Add(t.Name);
                        }
                    }
                    else
                    {
                        if (!diContainer.IsRegistered(t))
                        {
                            l.Add(t.Name);
                        }
                    }
                }

                ret = String.Join(", ", l);

                return ret;
            }


            private bool RunItemIfRunnable(bool isRunningSynchronouslyRequested, QueueItem item)
            {
                bool ret = false;

                if ((!item.IsAvailable && diContainer.IsRegistered(item.DependsOnTypeList)) ||
                    (item.IsAvailable && diContainer.IsAvailable(item.DependsOnTypeList)))
                {
                    // run action

                    itemExecuterAction(isRunningSynchronouslyRequested, item.ToString(), item.Action);

                    ret = true;
                }

                return ret;
            }




            private class QueueItem
            {
                public bool IsAvailable { get; private set; }
                public List<Type> DependsOnTypeList { get; private set; }
                public Action Action { get; private set; }

                private StackFrame frame = null;

                public QueueItem(bool isAvailable, List<Type> dependsOnTypeList, Action action)
                {
                    this.IsAvailable = isAvailable;
                    this.DependsOnTypeList = dependsOnTypeList;
                    this.Action = action;

                    frame = new StackTrace(4, true).GetFrame(0); 
                }

                public override string ToString()
                {
                    return String.Format("{0}.{1} ({2} at line {3})", this.Action.Method.DeclaringType.FullName, frame.GetMethod().Name, this.Action.Method.Name, frame.GetFileLineNumber());
                }
            }
        }
    }
}
