import SwiftUI

@main
struct KeeperLegacyApp: App {

    @Environment(\.scenePhase) private var scenePhase

    // Track when the app last went to background for decay calculation
    @AppStorage("lastBackgroundDate") private var lastBackgroundTimestamp: Double = Date().timeIntervalSince1970

    @StateObject private var habitatVM = HabitatViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
        }
        .onChange(of: scenePhase) { phase in
            switch phase {
            case .background:
                lastBackgroundTimestamp = Date().timeIntervalSince1970
            case .active:
                applyOfflineDecay()
            default:
                break
            }
        }
    }

    /// When the app returns to foreground, apply stat decay for however long it was away.
    private func applyOfflineDecay() {
        let now        = Date().timeIntervalSince1970
        let elapsed    = now - lastBackgroundTimestamp
        let hoursPassed = elapsed / 3600.0
        guard hoursPassed > 0.1 else { return }    // Skip tiny gaps
        Task { @MainActor in
            await habitatVM.load()
            habitatVM.applyDecayForAllCreatures(hoursPassed: min(hoursPassed, 72))
        }
        lastBackgroundTimestamp = now
    }
}
