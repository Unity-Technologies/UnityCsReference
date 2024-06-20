// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Threading;
using UnityEngine.Assertions;
using UnityEngine.LightTransport;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;
using static UnityEditor.LightBaking.LightBaker;

namespace UnityEditor.LightBaking
{
    internal static class UnityComputeBake
    {
        [RequiredByNativeCode]
        internal static bool BakeWithDummyProgress(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath)
        {
            using BakeProgressState dummyProgressState = new();
            bool success = Bake(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, dummyProgressState);

            return success;
        }

        [RequiredByNativeCode]
        internal static bool Bake(string bakeInputPath, string lightmapRequestsPath, string lightProbeRequestsPath, string bakeOutputFolderPath, BakeProgressState progressState)
        {
            Type strangler = Type.GetType("UnityEngine.PathTracing.LightBakerBridge.LightBakerStrangler, Unity.PathTracing.Runtime");
            if (strangler == null)
                return false;

            MethodInfo bakeMethod = strangler.GetMethod("Bake", BindingFlags.Static | BindingFlags.NonPublic);
            if (bakeMethod == null)
                return false;

            var bakeFunc = (Func<string, string, string, string, BakeProgressState, bool>)Delegate.CreateDelegate(typeof(Func<string, string, string, string, BakeProgressState, bool>), bakeMethod);
            return bakeFunc(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, progressState);
        }

        [RequiredByNativeCode]
        internal static bool BakeViaCommandLineParameters()
        {
            const string lightBakerWorkerProcess = "[LightBaker worker process] ";

            using ExternalProcessConnection bakeResultConnection = CreateConnectionToParentProcess("-bakePortNumber");
            using ExternalProcessConnection progressConnection = CreateConnectionToParentProcess("-progressPortNumber");

            try
            {
                // Report that we are connected.
                ReportResult(new Result { type = ResultType.ConnectedToBaker, message = "Successfully connected to the parent process." }, bakeResultConnection);

                string bakeInputPath = TryFindArgument("-bakeInput");
                if (string.IsNullOrEmpty(bakeInputPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No bake input path was passed as an argument." }, bakeResultConnection);

                string lightmapRequestsPath = TryFindArgument("-lightmapRequests");
                if (string.IsNullOrEmpty(lightmapRequestsPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No lightmap requests path was passed as an argument." }, bakeResultConnection);

                string lightProbeRequestsPath = TryFindArgument("-lightProbeRequests");
                if (string.IsNullOrEmpty(lightProbeRequestsPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No light probe requests path was passed as an argument." }, bakeResultConnection);

                string bakeOutputFolderPath = TryFindArgument("-bakeOutputFolderPath");
                if (string.IsNullOrEmpty(bakeOutputFolderPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No bake output folder path was passed as an argument." }, bakeResultConnection);

                // Prepare to capture progress work steps.
                using BakeProgressState progressState = new ();

                // Prepare to report progress.
                using CancellationTokenSource progressReporterTokenSource = new CancellationTokenSource();
                Thread progressReporterThread = new (() => ProgressReporterThreadFunction(progressState, progressReporterTokenSource.Token, progressConnection));
                progressReporterThread.Start();

                Result result = new ()
                {
                    type = Bake(bakeInputPath, lightmapRequestsPath, lightProbeRequestsPath, bakeOutputFolderPath, progressState) ? ResultType.Success : ResultType.JobFailed
                };

                // Report the result after cancel of the progress reporter when we are finished baking.
                progressReporterTokenSource.Cancel();
                ReportResult(result, bakeResultConnection);
            }
            catch (Exception e)
            {
                ReportResult(new Result { type = ResultType.JobFailed, message = $"An exception was thrown '{e.Message}'." }, bakeResultConnection);
            }
            finally
            {
                const int waitForTheResultToBeReceivedByTheCallerMs = 100;
                Thread.Sleep(waitForTheResultToBeReceivedByTheCallerMs);
            }

            return true;

            static ExternalProcessConnection CreateConnectionToParentProcess(string portNumberArgument)
            {
                string portNumberArgumentValue = TryFindArgument(portNumberArgument);
                bool portNumberWasPassed = !string.IsNullOrEmpty(portNumberArgumentValue);
                Debug.Log($"{lightBakerWorkerProcess}No '{portNumberArgument}' was passed as an argument, will not report to the parent process.");
                if (!portNumberWasPassed)
                    return null;
                bool portNumberWasParsed = int.TryParse(portNumberArgumentValue, out int portNumber);
                Assert.IsTrue(portNumberWasParsed, $"{lightBakerWorkerProcess}The '{portNumberArgument} passed as an argument was invalid, cannot report to the parent process.");
                if (!portNumberWasParsed)
                    return null;
                ExternalProcessConnection connection = new();
                bool connected = connection.Connect(portNumber);
                if (!connected)
                    Debug.Log($"{lightBakerWorkerProcess}Failed to connect to the parent process '{portNumberArgumentValue}'.");

                return connection;
            }

            static void ProgressReporterThreadFunction(BakeProgressState bakeProgressState, CancellationToken cancelToken, ExternalProcessConnection connection)
            {
                if (connection == null)
                    return;
                const int waitBetweenProgressReportsMs = 100;
                while (!cancelToken.IsCancellationRequested)
                {
                    float progress = bakeProgressState.Progress();
                    ReportProgressToParentProcess(progress, connection);
                    Thread.Sleep(waitBetweenProgressReportsMs);
                }
            }

            static string TryFindArgument(string argument)
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                    if (0 == string.Compare(args[i], argument, StringComparison.InvariantCultureIgnoreCase))
                        if (i < args.Length - 1 && !args[i + 1].StartsWith("-"))
                            return args[i + 1];

                return string.Empty;
            }

            static bool ReportResult(Result result, ExternalProcessConnection connection)
            {
                if (connection == null)
                    return true;
                if (result.type == ResultType.Success)
                    if (string.IsNullOrEmpty(result.message))
                        result.message = "Success.";
                bool sentResult = ReportResultToParentProcess(result, connection);
                Assert.IsTrue(sentResult, $"{lightBakerWorkerProcess}Failed to report a result to the parent process.");
                Debug.Log($"{lightBakerWorkerProcess}Reported result to the parent process: ({result.type}, '{result.message}')");

                return sentResult;
            }
        }
    }
}
