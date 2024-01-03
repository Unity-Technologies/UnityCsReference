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
        // TODO(pema.malling): Handle reporting back progress https://jira.unity3d.com/browse/LIGHT-1754
        [RequiredByNativeCode]
        internal static bool Bake(string bakeInputPath, string bakeOutputFolderPath)
        {
            Type strangler = Type.GetType("UnityEngine.PathTracing.LightBakerBridge.LightBakerStrangler, Unity.PathTracing.Runtime");
            if (strangler == null)
                return false;

            MethodInfo bakeMethod = strangler.GetMethod("Bake", BindingFlags.Static | BindingFlags.NonPublic);
            if (bakeMethod == null)
                return false;

            return (bool)bakeMethod.Invoke(null, new object[] { bakeInputPath, bakeOutputFolderPath });
        }

        [RequiredByNativeCode]
        internal static bool BakeViaCommandLineParameters()
        {
            const string lightBakerWorkerProcess = "[LightBaker worker process] ";

            using ExternalProcessConnection bakeResultConnection = CreateConnectionToParentProcess("-bakePortNumber");
            if (bakeResultConnection == default(ExternalProcessConnection))
                return false;
            using ExternalProcessConnection progressConnection = CreateConnectionToParentProcess("-progressPortNumber");
            if (progressConnection == default(ExternalProcessConnection))
                return false;

            try
            {
                // Report that we are connected.
                ReportResult(new Result { type = ResultType.ConnectedToBaker, message = "Successfully connected to the parent process." }, bakeResultConnection);

                string bakeInputPath = TryFindArgument("-bakeInput");
                if (string.IsNullOrEmpty(bakeInputPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No bake input path was passed as an argument." }, bakeResultConnection);

                string bakeOutputFolderPath = TryFindArgument("-bakeOutputFolderPath");
                if (string.IsNullOrEmpty(bakeOutputFolderPath))
                    return ReportResult(new Result { type = ResultType.InvalidInput, message = "No bake output folder path was passed as an argument." }, bakeResultConnection);

                // Prepare to capture progress work steps.
                BakeProgressState progress = new ();

                // Prepare to report progress.
                using CancellationTokenSource progressReporterTokenSource = new CancellationTokenSource();
                Thread progressReporterThread = new (() => ProgressReporterThreadFunction(progress, progressReporterTokenSource.Token, progressConnection));
                progressReporterThread.Start();

                Result result = new ()
                {
                    type = Bake(bakeInputPath, bakeOutputFolderPath) ? ResultType.Success : ResultType.JobFailed
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
                Assert.IsTrue(portNumberWasPassed, $"{lightBakerWorkerProcess}No '{portNumberArgument}' was passed as an argument, cannot report to the parent process.");
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
