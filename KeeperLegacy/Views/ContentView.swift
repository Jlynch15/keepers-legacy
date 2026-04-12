import SwiftUI

// MARK: - App Entry Point View
// Tab bar with 4 main sections.
// Injects DataManager and ViewModels into the environment.

struct ContentView: View {
    @StateObject private var shopVM      = ShopViewModel()
    @StateObject private var habitatVM   = HabitatViewModel()
    @StateObject private var progressVM  = ProgressionViewModel()

    @State private var selectedTab: Tab = .shop
    @State private var isLoading: Bool  = true

    enum Tab: Int {
        case shop, habitats, pedia, settings
    }

    var body: some View {
        ZStack {
            if isLoading {
                LaunchScreenView()
                    .transition(.opacity)
            } else {
                mainTabView
                    .transition(.opacity)
            }
        }
        .animation(.easeInOut(duration: 0.5), value: isLoading)
        .task {
            await loadGame()
        }
    }

    // MARK: Main Tab View

    private var mainTabView: some View {
        TabView(selection: $selectedTab) {
            ShopView()
                .environmentObject(shopVM)
                .environmentObject(progressVM)
                .tabItem {
                    Label("Shop", systemImage: "storefront")
                }
                .tag(Tab.shop)

            HabitatView()
                .environmentObject(habitatVM)
                .environmentObject(progressVM)
                .tabItem {
                    Label("Habitats", systemImage: "house.fill")
                }
                .tag(Tab.habitats)

            PediaView()
                .environmentObject(shopVM)
                .tabItem {
                    Label("Monsterpedia", systemImage: "book.fill")
                }
                .tag(Tab.pedia)

            SettingsView()
                .tabItem {
                    Label("Settings", systemImage: "gearshape.fill")
                }
                .tag(Tab.settings)
        }
        .tint(Color(hex: "#C99BFF"))    // Magical purple accent
        .overlay(alignment: .top) {
            CurrencyHeaderView()
                .environmentObject(progressVM)
        }
    }

    // MARK: Game Load

    private func loadGame() async {
        // Initialize Core Data / game state
        await shopVM.load()
        await habitatVM.load()
        await progressVM.load()

        try? await Task.sleep(nanoseconds: 1_200_000_000) // 1.2s minimum splash
        isLoading = false
    }
}

// MARK: - Currency Header (persistent top bar)

struct CurrencyHeaderView: View {
    @EnvironmentObject var progressVM: ProgressionViewModel

    var body: some View {
        HStack(spacing: 20) {
            // Coins
            HStack(spacing: 6) {
                Image(systemName: "circle.fill")
                    .foregroundColor(Color(hex: "#FFD700"))
                    .font(.system(size: 14))
                Text("\(progressVM.coins)")
                    .font(.system(size: 14, weight: .semibold, design: .rounded))
                    .foregroundColor(.primary)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(.ultraThinMaterial, in: Capsule())

            // Stardust
            HStack(spacing: 6) {
                Image(systemName: "sparkles")
                    .foregroundColor(Color(hex: "#C99BFF"))
                    .font(.system(size: 14))
                Text("\(progressVM.stardust)")
                    .font(.system(size: 14, weight: .semibold, design: .rounded))
                    .foregroundColor(.primary)
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(.ultraThinMaterial, in: Capsule())

            Spacer()

            // Level badge
            Text("Lv. \(progressVM.level)")
                .font(.system(size: 13, weight: .bold, design: .rounded))
                .foregroundColor(.white)
                .padding(.horizontal, 10)
                .padding(.vertical, 5)
                .background(Color(hex: "#C99BFF"), in: Capsule())
        }
        .padding(.horizontal, 16)
        .padding(.top, 8)
    }
}

// MARK: - Preview

#Preview {
    ContentView()
        .environmentObject(DataManager.preview)
}
