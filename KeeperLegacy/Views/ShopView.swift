import SwiftUI

// MARK: - Shop View
// Displays all creatures available for purchase, organized by habitat type.
// Unlocked creatures shown first; locked (story-gated) shown as silhouettes.

struct ShopView: View {
    @EnvironmentObject var shopVM:      ShopViewModel
    @EnvironmentObject var progressVM:  ProgressionViewModel

    @State private var selectedHabitat: HabitatType? = nil
    @State private var selectedCreature: CreatureCatalogEntry? = nil

    // Filtered creature list
    private var displayedCreatures: [CreatureCatalogEntry] {
        let all = CreatureCatalogEntry.allCreatures
        if let filter = selectedHabitat {
            return all.filter { $0.habitatType == filter }
        }
        return all
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                habitatFilterBar
                creatureGrid
            }
            .navigationTitle("Creature Shop")
            .navigationBarTitleDisplayMode(.large)
            .background(Color(hex: "#F5F0FF"))
            .sheet(item: $selectedCreature) { creature in
                CreaturePurchaseSheet(creature: creature)
                    .environmentObject(shopVM)
                    .environmentObject(progressVM)
            }
        }
    }

    // MARK: Habitat Filter Bar

    private var habitatFilterBar: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 10) {
                FilterChip(
                    label: "All",
                    color: Color(hex: "#C99BFF"),
                    isSelected: selectedHabitat == nil
                ) {
                    selectedHabitat = nil
                }

                ForEach(HabitatType.allCases, id: \.self) { type in
                    FilterChip(
                        label: type.rawValue,
                        color: Color(hex: type.displayColor),
                        isSelected: selectedHabitat == type,
                        locked: isHabitatTypeLocked(type)
                    ) {
                        selectedHabitat = type
                    }
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 12)
        }
        .background(.white.opacity(0.7))
    }

    // MARK: Creature Grid

    private var creatureGrid: some View {
        ScrollView {
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 150), spacing: 12)], spacing: 12) {
                ForEach(displayedCreatures) { creature in
                    CreatureShopCard(
                        creature: creature,
                        isLocked: isCreatureLocked(creature),
                        isOwned: shopVM.isOwned(catalogID: creature.id)
                    ) {
                        if !isCreatureLocked(creature) {
                            selectedCreature = creature
                        }
                    }
                }
            }
            .padding(16)
        }
    }

    // MARK: Lock Logic

    private func isHabitatTypeLocked(_ type: HabitatType) -> Bool {
        guard let requiredAct = type.requiresStoryAct else { return false }
        return progressVM.storyAct < requiredAct
    }

    private func isCreatureLocked(_ creature: CreatureCatalogEntry) -> Bool {
        isHabitatTypeLocked(creature.habitatType)
    }
}

// MARK: - Filter Chip

struct FilterChip: View {
    let label: String
    let color: Color
    let isSelected: Bool
    var locked: Bool = false
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 4) {
                if locked {
                    Image(systemName: "lock.fill")
                        .font(.system(size: 10))
                }
                Text(label)
                    .font(.system(size: 13, weight: .semibold, design: .rounded))
            }
            .padding(.horizontal, 14)
            .padding(.vertical, 8)
            .background(isSelected ? color : color.opacity(0.2))
            .foregroundColor(isSelected ? .white : .primary.opacity(locked ? 0.4 : 1.0))
            .clipShape(Capsule())
            .overlay(
                Capsule()
                    .stroke(color.opacity(0.4), lineWidth: isSelected ? 0 : 1)
            )
        }
        .disabled(locked)
    }
}

// MARK: - Creature Shop Card

struct CreatureShopCard: View {
    let creature: CreatureCatalogEntry
    let isLocked: Bool
    let isOwned: Bool
    let onTap: () -> Void

    var body: some View {
        Button(action: onTap) {
            VStack(spacing: 10) {
                // Creature portrait (placeholder until art assets are available)
                creaturePortrait

                // Name
                Text(creature.name)
                    .font(.system(size: 13, weight: .bold, design: .rounded))
                    .foregroundColor(isLocked ? .secondary : .primary)
                    .lineLimit(1)

                // Rarity badge
                RarityBadge(rarity: creature.rarity)

                // Price / owned / locked state
                priceLabel
            }
            .padding(12)
            .background(.white)
            .clipShape(RoundedRectangle(cornerRadius: 16))
            .shadow(color: .black.opacity(0.06), radius: 6, x: 0, y: 3)
            .overlay(
                RoundedRectangle(cornerRadius: 16)
                    .stroke(isOwned ? Color.green.opacity(0.5) : Color.clear, lineWidth: 2)
            )
        }
        .disabled(isLocked)
    }

    private var creaturePortrait: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 12)
                .fill(
                    LinearGradient(
                        colors: [
                            Color(hex: creature.habitatType.displayColor).opacity(0.3),
                            Color(hex: creature.habitatType.displayColor).opacity(0.6)
                        ],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )
                .frame(height: 100)

            if isLocked {
                Image(systemName: "lock.fill")
                    .font(.system(size: 32))
                    .foregroundColor(.secondary.opacity(0.5))
            } else {
                CreatureImageView(catalogID: creature.id, mutation: 0, size: 80)
            }
        }
    }

    private var priceLabel: some View {
        Group {
            if isLocked {
                Text("Locked")
                    .font(.system(size: 12, weight: .medium, design: .rounded))
                    .foregroundColor(.secondary)
            } else if isOwned {
                HStack(spacing: 4) {
                    Image(systemName: "checkmark.circle.fill")
                        .foregroundColor(.green)
                        .font(.system(size: 12))
                    Text("Owned")
                        .font(.system(size: 12, weight: .semibold, design: .rounded))
                        .foregroundColor(.green)
                }
            } else {
                HStack(spacing: 4) {
                    Image(systemName: "circle.fill")
                        .foregroundColor(Color(hex: "#FFD700"))
                        .font(.system(size: 10))
                    Text("\(creature.rarity.basePrice)")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                        .foregroundColor(.primary)
                }
            }
        }
    }

}

// MARK: - Rarity Badge

struct RarityBadge: View {
    let rarity: Rarity

    var color: Color {
        switch rarity {
        case .common:   return .gray
        case .uncommon: return Color(hex: "#A8D8EA")
        case .rare:     return Color(hex: "#C99BFF")
        }
    }

    var body: some View {
        Text(rarity.rawValue)
            .font(.system(size: 10, weight: .semibold, design: .rounded))
            .foregroundColor(.white)
            .padding(.horizontal, 8)
            .padding(.vertical, 3)
            .background(color, in: Capsule())
    }
}

// MARK: - Purchase Sheet

struct CreaturePurchaseSheet: View {
    @EnvironmentObject var shopVM:     ShopViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel
    @Environment(\.dismiss) private var dismiss

    let creature: CreatureCatalogEntry
    @State private var selectedMutation: Int = 0
    @State private var showInsufficientFunds = false

    var price: Int { creature.rarity.basePrice }
    var canAfford: Bool { progressVM.coins >= price }

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 24) {
                    // Creature preview
                    ZStack {
                        RoundedRectangle(cornerRadius: 20)
                            .fill(
                                LinearGradient(
                                    colors: [
                                        Color(hex: creature.habitatType.displayColor).opacity(0.4),
                                        Color(hex: creature.habitatType.displayColor)
                                    ],
                                    startPoint: .top,
                                    endPoint: .bottom
                                )
                            )
                            .frame(height: 200)

                        VStack(spacing: 6) {
                            CreatureImageView(catalogID: creature.id, mutation: selectedMutation, size: 120)
                            Text("Mutation \(selectedMutation + 1)")
                                .font(.system(size: 12, weight: .medium, design: .rounded))
                                .foregroundColor(.white.opacity(0.8))
                        }
                    }

                    // Mutation selector
                    if creature.mutations.count > 1 {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Choose Variant")
                                .font(.system(size: 14, weight: .semibold, design: .rounded))
                            ScrollView(.horizontal, showsIndicators: false) {
                                HStack(spacing: 10) {
                                    ForEach(creature.mutations, id: \.index) { mutation in
                                        MutationChip(
                                            mutation: mutation,
                                            isSelected: selectedMutation == mutation.index,
                                            habitatColor: Color(hex: creature.habitatType.displayColor)
                                        ) {
                                            selectedMutation = mutation.index
                                        }
                                    }
                                }
                            }
                        }
                        .padding(.horizontal)
                    }

                    // Details
                    VStack(alignment: .leading, spacing: 12) {
                        Text(creature.name)
                            .font(.system(size: 22, weight: .bold, design: .rounded))
                        Text(creature.description)
                            .font(.system(size: 15))
                            .foregroundColor(.secondary)
                        HStack {
                            Label(creature.habitatType.rawValue, systemImage: "house.fill")
                            Spacer()
                            RarityBadge(rarity: creature.rarity)
                        }
                        .font(.system(size: 14))
                    }
                    .padding(.horizontal)

                    // Purchase button
                    Button {
                        purchase()
                    } label: {
                        HStack(spacing: 8) {
                            Image(systemName: "circle.fill")
                                .foregroundColor(Color(hex: "#FFD700"))
                            Text("Buy for \(price) Coins")
                                .font(.system(size: 16, weight: .bold, design: .rounded))
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 16)
                        .background(canAfford ? Color(hex: "#C99BFF") : Color.gray.opacity(0.4))
                        .foregroundColor(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 16))
                    }
                    .disabled(!canAfford)
                    .padding(.horizontal)

                    if !canAfford {
                        Text("Not enough Coins. Earn more by selling creatures or fulfilling orders!")
                            .font(.system(size: 13))
                            .foregroundColor(.secondary)
                            .multilineTextAlignment(.center)
                            .padding(.horizontal)
                    }
                }
                .padding(.vertical, 20)
            }
            .navigationTitle("Purchase")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
    }

    private func purchase() {
        guard canAfford else { return }
        shopVM.purchase(
            creature: creature,
            mutationIndex: selectedMutation,
            cost: price,
            progressVM: progressVM
        )
        dismiss()
    }

}

struct MutationChip: View {
    let mutation: CreatureCatalogEntry.MutationVariant
    let isSelected: Bool
    let habitatColor: Color
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            Text(mutation.colorHint)
                .font(.system(size: 12, weight: .medium, design: .rounded))
                .padding(.horizontal, 12)
                .padding(.vertical, 6)
                .background(isSelected ? habitatColor : habitatColor.opacity(0.15))
                .foregroundColor(isSelected ? .white : .primary)
                .clipShape(Capsule())
        }
    }
}

// MARK: - Preview

#Preview {
    ShopView()
        .environmentObject(ShopViewModel())
        .environmentObject(ProgressionViewModel())
}
