import SwiftUI

// MARK: - Settings View

struct SettingsView: View {
    @AppStorage("iCloudSyncEnabled") private var iCloudSyncEnabled: Bool = true
    @AppStorage("soundEnabled")      private var soundEnabled:      Bool = true
    @AppStorage("musicEnabled")      private var musicEnabled:      Bool = true
    @AppStorage("notificationsEnabled") private var notificationsEnabled: Bool = true

    var body: some View {
        NavigationStack {
            List {
                // Cloud Sync
                Section("iCloud") {
                    Toggle("Sync with iCloud", isOn: $iCloudSyncEnabled)
                    if iCloudSyncEnabled {
                        Text("Your game saves to iCloud automatically.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }

                // Audio
                Section("Audio") {
                    Toggle("Sound Effects", isOn: $soundEnabled)
                    Toggle("Background Music", isOn: $musicEnabled)
                }

                // Notifications
                Section("Notifications") {
                    Toggle("Creature Care Reminders", isOn: $notificationsEnabled)
                    if notificationsEnabled {
                        Text("Get reminded when your creatures need care.")
                            .font(.caption)
                            .foregroundColor(.secondary)
                    }
                }

                // About
                Section("About") {
                    HStack {
                        Text("Version")
                        Spacer()
                        Text("1.0.0 (Phase 1)")
                            .foregroundColor(.secondary)
                    }
                    HStack {
                        Text("Creatures")
                        Spacer()
                        Text("58 species, 232 variants")
                            .foregroundColor(.secondary)
                    }
                    Link("Privacy Policy", destination: URL(string: "https://example.com/privacy")!)
                    Link("Support", destination: URL(string: "https://example.com/support")!)
                }
            }
            .navigationTitle("Settings")
        }
    }
}

#Preview {
    SettingsView()
}
