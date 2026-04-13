import SwiftUI

// MARK: - Monsterpedia View
// Grid of all 58 creature species. Discovered = full card, undiscovered = silhouette.
// Tracks collection progress per habitat type.

struct PediaView: View {
    @EnvironmentObject var shopVM: ShopViewModel

    @State private var selectedHabitat: HabitatType? = nil
    @State private var searchText: String = ""
    @State private var selectedEntry: CreatureCatalogEntry? = nil

    private var displayedEntries: [CreatureCatalogEntry] {
        var entries = CreatureCatalogEntry.allCreatures
        if let filter = selectedHabitat {
            entries = entries.filter { $0.habitatType == filter }
        }
        if !searchText.isEmpty {
            entries = entries.filter {
                $0.name.localizedCaseInsensitiveContains(searchText)
            }
        }
        return entries
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                collectionProgress
                filterBar
                creatureGrid
            }
            .navigationTitle("Monsterpedia")
            .navigationBarTitleDisplayMode(.large)
            .searchable(text: $searchText, prompt: "Search creatures...")
            .background(Color(hex: "#FFF5F8"))
            .sheet(item: $selectedEntry) { entry in
                PediaDetailView(entry: entry, isDiscovered: shopVM.hasDiscovered(catalogID: entry.id))
            }
        }
    }

    // MARK: Collection Progress Bar

    private var collectionProgress: some View {
        let total     = CreatureCatalogEntry.allCreatures.count
        let discovered = shopVM.discoveredCatalogIDs.count
        let fraction  = total > 0 ? Double(discovered) / Double(total) : 0

        return VStack(spacing: 6) {
            HStack {
                Text("Collection")
                    .font(.system(size: 14, weight: .semibold, design: .rounded))
                Spacer()
                Text("\(discovered) / \(total)")
                    .font(.system(size: 13, design: .rounded))
                    .foregroundColor(.secondary)
            }
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    RoundedRectangle(cornerRadius: 4)
                        .fill(Color(hex: "#C99BFF").opacity(0.2))
                    RoundedRectangle(cornerRadius: 4)
                        .fill(
                            LinearGradient(
                                colors: [Color(hex: "#C99BFF"), Color(hex: "#7FFFD4")],
                                startPoint: .leading, endPoint: .trailing
                            )
                        )
                        .frame(width: geo.size.width * fraction)
                }
            }
            .frame(height: 8)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .background(.white.opacity(0.7))
    }

    // MARK: Filter Bar

    private var filterBar: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 10) {
                FilterChip(label: "All", color: Color(hex: "#C99BFF"), isSelected: selectedHabitat == nil) {
                    selectedHabitat = nil
                }
                ForEach(HabitatType.allCases, id: \.self) { type in
                    let count = shopVM.discoveredCount(ofType: type)
                    let total = CreatureCatalogEntry.creatures(ofType: type).count
                    FilterChip(
                        label: "\(type.rawValue) \(count)/\(total)",
                        color: Color(hex: type.displayColor),
                        isSelected: selectedHabitat == type
                    ) {
                        selectedHabitat = type
                    }
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 10)
        }
        .background(.white.opacity(0.6))
    }

    // MARK: Creature Grid

    private var creatureGrid: some View {
        ScrollView {
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 100), spacing: 10)], spacing: 10) {
                ForEach(displayedEntries) { entry in
                    PediaCard(
                        entry: entry,
                        isDiscovered: shopVM.hasDiscovered(catalogID: entry.id)
                    ) {
                        selectedEntry = entry
                    }
                }
            }
            .padding(14)
        }
    }
}

// MARK: - Pedia Card

struct PediaCard: View {
    let entry: CreatureCatalogEntry
    let isDiscovered: Bool
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            VStack(spacing: 6) {
                ZStack {
                    RoundedRectangle(cornerRadius: 12)
                        .fill(
                            isDiscovered
                            ? Color(hex: entry.habitatType.displayColor).opacity(0.35)
                            : Color.gray.opacity(0.1)
                        )
                        .frame(height: 80)

                    if isDiscovered {
                        CreatureImageView(catalogID: entry.id, mutation: 0, size: 56)
                    } else {
                        Image(systemName: "questionmark")
                            .font(.system(size: 28, weight: .bold))
                            .foregroundColor(.gray.opacity(0.3))
                    }
                }

                Text(isDiscovered ? entry.name : "???")
                    .font(.system(size: 10, weight: .semibold, design: .rounded))
                    .foregroundColor(isDiscovered ? .primary : .secondary)
                    .lineLimit(1)

                if isDiscovered {
                    RarityBadge(rarity: entry.rarity)
                }
            }
        }
    }

}

// MARK: - Pedia Detail Sheet

struct PediaDetailView: View {
    @Environment(\.dismiss) private var dismiss
    let entry: CreatureCatalogEntry
    let isDiscovered: Bool

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 20) {
                    // Header portrait
                    ZStack {
                        RoundedRectangle(cornerRadius: 20)
                            .fill(
                                LinearGradient(
                                    colors: [
                                        Color(hex: entry.habitatType.displayColor).opacity(isDiscovered ? 0.5 : 0.1),
                                        Color(hex: entry.habitatType.displayColor).opacity(isDiscovered ? 0.8 : 0.2)
                                    ],
                                    startPoint: .top, endPoint: .bottom
                                )
                            )
                            .frame(height: 200)

                        if isDiscovered {
                            CreatureImageView(catalogID: entry.id, mutation: 0, size: 130)
                        } else {
                            Image(systemName: "questionmark.circle.fill")
                                .font(.system(size: 64))
                                .foregroundColor(.gray.opacity(0.3))
                        }
                    }
                    .padding(.horizontal)

                    // Details
                    VStack(alignment: .leading, spacing: 14) {
                        HStack {
                            Text(isDiscovered ? entry.name : "Undiscovered")
                                .font(.system(size: 24, weight: .bold, design: .rounded))
                            Spacer()
                            if isDiscovered { RarityBadge(rarity: entry.rarity) }
                        }

                        if isDiscovered {
                            Text(entry.description)
                                .font(.system(size: 15))
                                .foregroundColor(.secondary)

                            Divider()

                            InfoRow(icon: "house.fill",    label: "Habitat",      value: entry.habitatType.rawValue)
                            InfoRow(icon: "star.fill",     label: "Rarity",       value: entry.rarity.rawValue)
                            InfoRow(icon: "gamecontroller.fill", label: "Favorite Toy", value: "Discover through play!")

                            Divider()

                            Text("Variants (\(entry.mutations.count))")
                                .font(.system(size: 14, weight: .semibold, design: .rounded))
                            ScrollView(.horizontal, showsIndicators: false) {
                                HStack(spacing: 10) {
                                    ForEach(entry.mutations, id: \.index) { mutation in
                                        VStack(spacing: 4) {
                                            CreatureImageView(catalogID: entry.id, mutation: mutation.index, size: 52)
                                            Text(mutation.colorHint)
                                                .font(.system(size: 10, design: .rounded))
                                                .padding(.horizontal, 8)
                                                .padding(.vertical, 4)
                                                .background(Color(hex: entry.habitatType.displayColor).opacity(0.2))
                                                .clipShape(Capsule())
                                        }
                                    }
                                }
                            }
                        } else {
                            Text("Purchase or breed this creature to discover its secrets.")
                                .font(.system(size: 15))
                                .foregroundColor(.secondary)
                        }
                    }
                    .padding(.horizontal)
                }
                .padding(.vertical, 20)
            }
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Close") { dismiss() }
                }
            }
        }
    }

}

struct InfoRow: View {
    let icon: String
    let label: String
    let value: String

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: icon)
                .foregroundColor(Color(hex: "#C99BFF"))
                .frame(width: 20)
            Text(label)
                .font(.system(size: 14, weight: .medium, design: .rounded))
                .foregroundColor(.secondary)
            Spacer()
            Text(value)
                .font(.system(size: 14, weight: .semibold, design: .rounded))
        }
    }
}

// MARK: - Preview

#Preview {
    PediaView()
        .environmentObject(ShopViewModel())
}
