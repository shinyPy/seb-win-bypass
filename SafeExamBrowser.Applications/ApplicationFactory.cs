﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using SafeExamBrowser.Applications.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Settings.Applications;

namespace SafeExamBrowser.Applications
{
	public class ApplicationFactory : IApplicationFactory
	{
		private ILogger logger;

		public ApplicationFactory(ILogger logger)
		{
			this.logger = logger;
		}

		public FactoryResult TryCreate(WhitelistApplication settings, out IApplication application)
		{
			application = default(IApplication);

			try
			{
				var success = TryFindMainExecutable(settings, out var mainExecutable);

				if (success)
				{
					application = new ExternalApplication();
					logger.Debug($"Successfully initialized application '{settings.DisplayName}' ({settings.ExecutableName}).");

					return FactoryResult.Success;
				}

				logger.Error($"Could not find application '{settings.DisplayName}' ({settings.ExecutableName})!");

				return FactoryResult.NotFound;
			}
			catch (Exception e)
			{
				logger.Error($"Unexpected error while trying to create application '{settings.DisplayName}' ({settings.ExecutableName})!", e);
			}

			return FactoryResult.Error;
		}

		private bool TryFindMainExecutable(WhitelistApplication settings, out string mainExecutable)
		{
			var paths = new List<string[]>();
			var registryPath = QueryPathFromRegistry(settings);

			mainExecutable = default(string);

			paths.Add(new[] { "%ProgramW6432%", settings.ExecutableName });
			paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), settings.ExecutableName });
			paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.System), settings.ExecutableName });
			paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), settings.ExecutableName });

			if (settings.ExecutablePath != default(string))
			{
				paths.Add(new[] { settings.ExecutablePath, settings.ExecutableName });
				paths.Add(new[] { "%ProgramW6432%", settings.ExecutablePath, settings.ExecutableName });
				paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), settings.ExecutablePath, settings.ExecutableName });
				paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.System), settings.ExecutablePath, settings.ExecutableName });
				paths.Add(new[] { Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), settings.ExecutablePath, settings.ExecutableName });
			}

			if (registryPath != default(string))
			{
				paths.Add(new[] { registryPath, settings.ExecutableName });

				if (settings.ExecutablePath != default(string))
				{
					paths.Add(new[] { registryPath, settings.ExecutablePath, settings.ExecutableName });
				}
			}

			foreach (var path in paths)
			{
				try
				{
					mainExecutable = Path.Combine(path);
					mainExecutable = Environment.ExpandEnvironmentVariables(mainExecutable);

					if (File.Exists(mainExecutable))
					{
						return true;
					}
				}
				catch (Exception e)
				{
					logger.Error($"Failed to test path {string.Join(@"\", path)}!", e);
				}
			}

			return false;
		}

		private string QueryPathFromRegistry(WhitelistApplication settings)
		{
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{settings.ExecutableName}"))
				{
					if (key != null)
					{
						return key.GetValue("Path") as string;
					}
				}
			}
			catch (Exception e)
			{
				logger.Error($"Failed to query path in registry for '{settings.ExecutableName}'!", e);
			}

			return default(string);
		}
	}
}
