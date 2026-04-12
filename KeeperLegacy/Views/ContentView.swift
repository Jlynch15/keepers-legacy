import SwiftUI

// MARK: - App Entry Point View
// 5-tab layout: Shop, Orders, Habitats, Monsterpedia, Settings

struct ContentView: View {
    @StateObject private var shopVM      = ShopViewModel()
    @StateObject private var habitatVM   = HabitatViewModel()
    @StateObject private var progressVM  = ProgressionViewModel()
    @StateObject private var orderVM     = CustomerOrderViewModel()
    @StateObject private var creatureVM  = CreatureViewModel()
    @StateObject private var storyVM     = StoryViewModel()

    @State private var selectedTab: Tab  = .shop
    @State private var isLoading: Bool   = true

    enum Tab: Int {
        case shop, orders, habitats, pedia, settings
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

            // Story event overlay — shown when a story event triggers
            if let event = storyVM.pendingStoryEvent {
                StoryEventView(event: event)
                    .environmentObject(storyVM)
                    .environmentObject(progressVM)
                    .zIndex(99)
            }

            // Level-up overlay — shown on top of story events
            if let event = progressVM.pendingLevelUp {
                LevelUpOverlayView(event: event) {
                    progressVM.pendingLevelUp = nil
                    // After level-up dismisses, check if a story event should fire
                    storyVM.checkTriggers(level: progressVM.level)
                }
                .zIndex(100)
            }
        }
        .animation(.easeInOut(duration: 0.5), value: isLoading)
        .task { await loadGame() }
    }

    // MARK: Main Tab View

    private var mainTabView: some View {
        VStack(spacing: 0) {
            // Persistent currency + level header
            CurrencyHeaderView()
                .environmentObject(progressVM)

            TabView(selection: $selectedTab) {
                ShopView()
                    .environmentObject(shopVM)
                    .environmentObject(progressVM)
                    .tabItem { Label("Shop", systemImage: "storefront") }
                    .tag(Tab.shop)

                OrdersView()
                    .environmentObject(orderVM)
                    .environmentObject(progressVM)
                    .tabItem {
                        Label("Orders", systemImage: "bag.fill")
                    }
                    .badge(orderVM.activeOrders.filter { orderVM.canFulfill($0) }.count)
                    .tag(Tab.orders)

                HabitatView()
                    .environmentObject(habitatVM)
                    .environmentObject(progressVM)
                    .environmentObject(creatureVM)
                    .tabItem { Label("Habitats", systemImage: "house.fill") }
                    .tag(Tab.habitats)

                PediaView()
                    .environmentObject(shopVM)
                    .tabItem { Label("Pedia", systemImage: "book.fill") }
                    .tag(Tab.pedia)

                SettingsView()
                    .environmentObject(storyVM)
                    .tabItem { Label("Settings", systemImage: "gearshape.fill") }
                    .tag(Tab.settings)
            }
            .tint(Color(hex: "#C99BFF"))
        }
        .ignoresSafeArea(edges: .bottom)
    }

    // MARK: Game Load

    private func loadGame() async {
        await progressVM.load()
        await shopVM.load()
        await habitatVM.load()
        await orderVM.load(discoveredIDs: shopVM.discoveredCatalogIDs)
        storyVM.load()

        try? await Task.sleep(nanoseconds: 1_200_000_000)
        isLoading = false

        // Check for any story events that should fire immediately on load
        // (e.g. act1_first_egg fires at level 1 — will trigger on very first launch)
        storyVM.checkTriggers(level: progressVM.level)
    }
}

// MARK: - Currency Header

struct CurrencyHeaderView: View {
    @EnvironmentObject var progressVM: ProgressionViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Coins
            HStack(spacing: 5) {
                Image(systemName: "circle.fill")
                    .foregroundColor(Color(hex: "#FFD700"))
                    .font(.system(size: 13))
                Text("\(progressVM.coins)")
                    .font(.system(size: 14, weight: .semibold, design: .rounded))
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(.ultraThinMaterial, in: Capsule())

            // Stardust
            HStack(spacing: 5) {
                Image(systemName: "sparkles")
                    .foregroundColor(Color(hex: "#C99BFF"))
                    .font(.system(size: 13))
                Text("\(progressVM.stardust)")
                    .font(.system(size: 14, weight: .semibold, design: .rounded))
            }
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(.ultraThinMaterial, in: Capsule())

            Spacer()

            // XP bar + level
            VStack(alignment: .trailing, spacing: 2) {
                Text("Lv. \(progressVM.level)")
                    .font(.system(size: 12, weight: .bold, design: .rounded))
                    .foregroundColor(Color(hex: "#C99BFF"))
                GeometryReader { geo in
                    ZStack(alignment: .leading) {
                        Capsule()
                            .fill(Color(hex: "#C99BFF").opacity(0.2))
                        Capsule()
                            .fill(Color(hex: "#C99BFF"))
                            .frame(width: geo.size.width * CGFloat(progressVM.xpFraction))
                    }
                }
                .frame(width: 60, height: 5)
            }
        }
        .padding(.horizontal, 16)
        .padding(.top, 8)
        .padding(.bottom, 6)
        .background(.regularMaterial)
    }
}

// MARK: - Preview

#Preview {
    ContentView()
}
