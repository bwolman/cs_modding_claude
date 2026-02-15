using System;

using Colossal.Logging;

using Game;
using Game.Modding;

namespace ModName
{
    /// <summary>
    /// Main entry point for the ModName mod.
    /// </summary>
    public sealed class Mod : IMod
    {
        /// <summary>
        /// Shared logger instance for the mod.
        /// </summary>
        internal static ILog Log { get; } = LogManager
            .GetLogger(nameof(ModName))
            .SetShowsErrorsInUI(true);

        /// <summary>
        /// Called when the mod is loaded by the game.
        /// Register systems, settings, and resources here.
        /// </summary>
        /// <param name="updateSystem">The update system to register game systems with.</param>
        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            try
            {
                // Register your systems here:
                // updateSystem.UpdateAt<MySystem>(SystemUpdatePhase.GameSimulation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed during mod initialization");
            }
        }

        /// <summary>
        /// Called when the mod is unloaded.
        /// Clean up resources here.
        /// </summary>
        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
        }
    }
}
