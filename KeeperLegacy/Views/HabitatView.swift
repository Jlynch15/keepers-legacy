import SwiftUI

// MARK: - Habitat View
// Shows the player's habitat slots and the creatures living in them.
// Provides Feed, Play, Clean, and Sell actions per creature.

struct HabitatView: View {
    @EnvironmentObject var habitatVM:  HabitatViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel

    @State private var selectedHabitatIndex: Int = 0
    @State private var showCareSheet: Bool = false
    @State private var showHabitatTypePicker: Bool = false

    private var habitats: [HabitatEntity] { habitatVM.habitats }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                if habitats.isEmpty {
                    emptyState
                } else {
                    habitatSelector
                    habitatPanel
                }
            }
            .navigationTitle("My Habitats")
            .navigationBarTitleDisplayMode(.large)
            .background(Color(hex: "#F0F8FF"))
            .sheet(isPresented: $showCareSheet) {
                if let habitat = habitats[safe: selectedHabitatIndex],
                   let occupantID = habitat.occupantID,
                   let creature = habitatVM.creature(withID: occupantID) {
                    CareSheet(creatureEntity: creature)
                        .environmentObject(habitatVM)
                        .environmentObject(progressVM)
                }
            }
            .sheet(isPresented: $showHabitatTypePicker) {
                HabitatTypePickerView()
                    .environmentObject(habitatVM)
                    .environmentObject(progressVM)
            }
        }
    }

    // MARK: Habitat Tab Selector

    private var habitatSelector: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 10) {
                ForEach(Array(habitats.enumerated()), id: \.element.id) { index, habitat in
                    HabitatTab(
                        habitat: habitat,
                        isSelected: selectedHabitatIndex == index
                    ) {
                        selectedHabitatIndex = index
                    }
                }

                // "Add Habitat" slot (if expansions available)
                if habitatVM.canAddHabitat(atLevel: progressVM.level) {
                    AddHabitatButton {
                        showHabitatTypePicker = true
                    }
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 12)
        }
        .background(.white.opacity(0.8))
    }

    // MARK: Habitat Panel

    @ViewBuilder
    private var habitatPanel: some View {
        if let habitat = habitats[safe: selectedHabitatIndex] {
            if let occupantID = habitat.occupantID,
               let creature = habitatVM.creature(withID: occupantID) {
                OccupiedHabitatPanel(
                    habitat: habitat,
                    creature: creature
                ) {
                    showCareSheet = true
                }
                .environmentObject(habitatVM)
                .environmentObject(progressVM)
            } else {
                EmptyHabitatPanel(habitat: habitat)
                    .environmentObject(habitatVM)
            }
        }
    }

    // MARK: Empty State

    private var emptyState: some View {
        VStack(spacing: 20) {
            Image(systemName: "house")
                .font(.system(size: 64))
                .foregroundColor(Color(hex: "#C99BFF").opacity(0.5))
            Text("No habitats yet!")
                .font(.system(size: 20, weight: .bold, design: .rounded))
            Text("Visit the Shop to purchase your first creature,\nthen a habitat will be waiting for it.")
                .font(.system(size: 15))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
        }
        .padding()
    }
}

// MARK: - Habitat Tab Button

struct HabitatTab: View {
    let habitat: HabitatEntity
    let isSelected: Bool
    let action: () -> Void

    private var type: HabitatType {
        HabitatType(rawValue: habitat.type ?? "Water") ?? .water
    }

    var body: some View {
        Button(action: action) {
            VStack(spacing: 4) {
                Circle()
                    .fill(Color(hex: type.displayColor).opacity(isSelected ? 1.0 : 0.4))
                    .frame(width: 36, height: 36)
                    .overlay(
                        Image(systemName: habitatIcon)
                            .font(.system(size: 16))
                            .foregroundColor(.white)
                    )
                Text(type.rawValue)
                    .font(.system(size: 10, weight: isSelected ? .bold : .regular, design: .rounded))
                    .foregroundColor(isSelected ? .primary : .secondary)
            }
            .padding(8)
            .background(isSelected ? Color(hex: type.displayColor).opacity(0.15) : .clear)
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
    }

    private var habitatIcon: String {
        switch type {
        case .water:    return "drop.fill"
        case .dirt:     return "mountain.2.fill"
        case .grass:    return "leaf.fill"
        case .fire:     return "flame.fill"
        case .ice:      return "snowflake"
        case .electric: return "bolt.fill"
        case .magical:  return "sparkles"
        }
    }
}

struct AddHabitatButton: View {
    let action: () -> Void
    var body: some View {
        Button(action: action) {
            VStack(spacing: 4) {
                Circle()
                    .stroke(Color(hex: "#C99BFF"), style: StrokeStyle(lineWidth: 2, dash: [4]))
                    .frame(width: 36, height: 36)
                    .overlay(
                        Image(systemName: "plus")
                            .font(.system(size: 16))
                            .foregroundColor(Color(hex: "#C99BFF"))
                    )
                Text("Add")
                    .font(.system(size: 10, design: .rounded))
                    .foregroundColor(Color(hex: "#C99BFF"))
            }
            .padding(8)
        }
    }
}

// MARK: - Occupied Habitat Panel

struct OccupiedHabitatPanel: View {
    @EnvironmentObject var habitatVM:  HabitatViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel
    @EnvironmentObject var creatureVM: CreatureViewModel

    let habitat: HabitatEntity
    let creature: CreatureEntity
    let onCare: () -> Void

    @State private var showBreedingSheet: Bool = false

    private var catalogEntry: CreatureCatalogEntry? {
        CreatureCatalogEntry.find(byID: creature.catalogID ?? "")
    }

    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                // Creature display with mutation badge
                creatureDisplay

                // Stat bars
                if let entry = catalogEntry {
                    StatBarsView(creature: creature, entry: entry)
                }

                // Action buttons
                HStack(spacing: 12) {
                    CareButton(icon: "fork.knife", label: "Feed",  color: Color(hex: "#A8D5A8")) { onCare() }
                    CareButton(icon: "gamecontroller.fill", label: "Play",  color: Color(hex: "#A8D8EA")) { onCare() }
                    CareButton(icon: "sparkles",   label: "Clean", color: Color(hex: "#F0E68C")) { onCare() }
                    CareButton(icon: "dollarsign.circle.fill", label: "Sell",  color: Color(hex: "#FFB347")) {
                        habitatVM.sellCreature(creature, habitat: habitat, progressVM: progressVM)
                    }
                }
                .padding(.horizontal)

                // Breed button — only visible once feature is unlocked
                if progressVM.isFeatureUnlocked(.breeding) {
                    Button { showBreedingSheet = true } label: {
                        HStack(spacing: 8) {
                            Image(systemName: "heart.fill")
                            Text("Breed")
                                .font(.system(size: 15, weight: .bold, design: .rounded))
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 12)
                        .background(Color(hex: "#FFF0FF"))
                        .foregroundColor(Color(hex: "#C99BFF"))
                        .clipShape(RoundedRectangle(cornerRadius: 14))
                        .overlay(
                            RoundedRectangle(cornerRadius: 14)
                                .stroke(Color(hex: "#C99BFF").opacity(0.4), lineWidth: 1.5)
                        )
                    }
                    .padding(.horizontal)
                    .disabled(creature.lifecycle != LifecycleStage.adult.rawValue)
                }
            }
            .padding(.vertical, 20)
        }
        .sheet(isPresented: $showBreedingSheet) {
            BreedingView(parentA: creature)
                .environmentObject(creatureVM)
                .environmentObject(progressVM)
                .environmentObject(habitatVM)
        }
    }

    private var creatureDisplay: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 24)
                .fill(
                    LinearGradient(
                        colors: [
                            Color(hex: catalogEntry?.habitatType.displayColor ?? "#C99BFF").opacity(0.3),
                            Color(hex: catalogEntry?.habitatType.displayColor ?? "#C99BFF").opacity(0.6)
                        ],
                        startPoint: .top, endPoint: .bottom
                    )
                )
                .frame(height: 220)

            VStack(spacing: 8) {
                // Placeholder creature art
                Text(habitatEmoji)
                    .font(.system(size: 80))

                Text(catalogEntry?.name ?? "Unknown")
                    .font(.system(size: 18, weight: .bold, design: .rounded))
                    .foregroundColor(.white)

                if let entry = catalogEntry {
                    RarityBadge(rarity: entry.rarity)
                }
            }
        }
        .overlay(alignment: .topTrailing) {
            Text("V\(creature.mutationIndex + 1)")
                .font(.system(size: 12, weight: .bold, design: .rounded))
                .foregroundColor(.white)
                .padding(.horizontal, 10)
                .padding(.vertical, 5)
                .background(Color.black.opacity(0.3))
                .clipShape(Capsule())
                .padding(14)
        }
        .padding(.horizontal)
    }

    private var habitatEmoji: String {
        switch HabitatType(rawValue: habitat.type ?? "Water") ?? .water {
        case .water:    return "🌊"
        case .dirt:     return "🪨"
        case .grass:    return "🌿"
        case .fire:     return "🔥"
        case .ice:      return "❄️"
        case .electric: return "⚡"
        case .magical:  return "✨"
        }
    }
}

// MARK: - Stat Bars

struct StatBarsView: View {
    let creature: CreatureEntity
    let entry: CreatureCatalogEntry

    var body: some View {
        VStack(spacing: 10) {
            StatBar(label: "Hunger",      value: creature.hunger,      color: Color(hex: "#A8D5A8"), icon: "fork.knife")
            StatBar(label: "Happiness",   value: creature.happiness,   color: Color(hex: "#FFD700"), icon: "face.smiling")
            StatBar(label: "Cleanliness", value: creature.cleanliness, color: Color(hex: "#A8D8EA"), icon: "sparkles")
            StatBar(label: "Affection",   value: creature.affection,   color: Color(hex: "#FF9AA2"), icon: "heart.fill")
            StatBar(label: "Playfulness", value: creature.playfulness, color: Color(hex: "#C99BFF"), icon: "gamecontroller.fill")
        }
        .padding(.horizontal)
    }
}

struct StatBar: View {
    let label: String
    let value: Double
    let color: Color
    let icon: String

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: icon)
                .font(.system(size: 12))
                .foregroundColor(color)
                .frame(width: 20)

            Text(label)
                .font(.system(size: 12, weight: .medium, design: .rounded))
                .foregroundColor(.secondary)
                .frame(width: 80, alignment: .leading)

            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    RoundedRectangle(cornerRadius: 4)
                        .fill(color.opacity(0.2))
                    RoundedRectangle(cornerRadius: 4)
                        .fill(color)
                        .frame(width: geo.size.width * CGFloat(value))
                }
            }
            .frame(height: 8)

            Text("\(Int(value * 100))%")
                .font(.system(size: 11, design: .monospaced))
                .foregroundColor(.secondary)
                .frame(width: 36, alignment: .trailing)
        }
    }
}

// MARK: - Care Button

struct CareButton: View {
    let icon: String
    let label: String
    let color: Color
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            VStack(spacing: 6) {
                Circle()
                    .fill(color.opacity(0.2))
                    .frame(width: 50, height: 50)
                    .overlay(
                        Image(systemName: icon)
                            .font(.system(size: 20))
                            .foregroundColor(color)
                    )
                Text(label)
                    .font(.system(size: 11, weight: .semibold, design: .rounded))
                    .foregroundColor(.secondary)
            }
            .frame(maxWidth: .infinity)
        }
    }
}

// MARK: - Empty Habitat Panel

struct EmptyHabitatPanel: View {
    @EnvironmentObject var habitatVM: HabitatViewModel
    let habitat: HabitatEntity

    private var type: HabitatType {
        HabitatType(rawValue: habitat.type ?? "Water") ?? .water
    }

    var body: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "plus.circle.dashed")
                .font(.system(size: 64))
                .foregroundColor(Color(hex: type.displayColor).opacity(0.5))
            Text("Empty \(type.rawValue) Habitat")
                .font(.system(size: 18, weight: .bold, design: .rounded))
            Text("Visit the Shop to find a creature that\n loves the \(type.rawValue.lowercased()) environment.")
                .font(.system(size: 15))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
            Spacer()
        }
        .padding()
    }
}

// MARK: - Care Sheet (Feed/Play/Clean modal)

struct CareSheet: View {
    @EnvironmentObject var habitatVM:  HabitatViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel
    @Environment(\.dismiss) private var dismiss

    let creatureEntity: CreatureEntity

    private var catalogEntry: CreatureCatalogEntry? {
        CreatureCatalogEntry.find(byID: creatureEntity.catalogID ?? "")
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Text(catalogEntry?.name ?? "Creature")
                    .font(.system(size: 22, weight: .bold, design: .rounded))

                // Food selection
                VStack(alignment: .leading, spacing: 12) {
                    Text("Feed")
                        .font(.system(size: 16, weight: .semibold, design: .rounded))
                    ForEach(PricingTable.Food.catalog, id: \.id) { food in
                        FoodItemRow(food: food) {
                            habitatVM.feed(creature: creatureEntity, food: food, progressVM: progressVM)
                            dismiss()
                        }
                        .disabled(progressVM.coins < food.coinCost)
                    }
                }

                Divider()

                // Toy selection
                VStack(alignment: .leading, spacing: 12) {
                    Text("Play")
                        .font(.system(size: 16, weight: .semibold, design: .rounded))
                    ForEach(availableToys, id: \.self) { toy in
                        ToyItemRow(
                            toyName: toy,
                            isFavorite: toy == catalogEntry?.favoriteToy && creatureEntity.discoveredFavoriteToy
                        ) {
                            habitatVM.play(creature: creatureEntity, toy: toy, entry: catalogEntry, progressVM: progressVM)
                            dismiss()
                        }
                    }
                }

                Spacer()
            }
            .padding(20)
            .navigationTitle("Care")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Done") { dismiss() }
                }
            }
        }
    }

    private var availableToys: [String] {
        ["Ball", "Ribbon", "Squeaky Toy", "Puzzle", "Rope"]
    }
}

struct FoodItemRow: View {
    let food: PricingTable.Food
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack {
                Text(food.name)
                    .font(.system(size: 14, design: .rounded))
                Spacer()
                Text("+\(Int(food.hungerRestored * 100))% hunger")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
                HStack(spacing: 4) {
                    Image(systemName: "circle.fill")
                        .foregroundColor(Color(hex: "#FFD700"))
                        .font(.system(size: 10))
                    Text("\(food.coinCost)")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                }
                .padding(.horizontal, 10)
                .padding(.vertical, 5)
                .background(Color(hex: "#FFD700").opacity(0.15), in: Capsule())
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
            .background(.white)
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
    }
}

struct ToyItemRow: View {
    let toyName: String
    let isFavorite: Bool
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack {
                Text(toyName)
                    .font(.system(size: 14, design: .rounded))
                if isFavorite {
                    Image(systemName: "heart.fill")
                        .foregroundColor(.pink)
                        .font(.system(size: 12))
                    Text("Favorite!")
                        .font(.system(size: 11, weight: .semibold))
                        .foregroundColor(.pink)
                }
                Spacer()
                Text("Free")
                    .font(.system(size: 12))
                    .foregroundColor(.secondary)
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
            .background(.white)
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
    }
}

// MARK: - Safe subscript

extension Array {
    subscript(safe index: Int) -> Element? {
        indices.contains(index) ? self[index] : nil
    }
}

// MARK: - Preview

#Preview {
    HabitatView()
        .environmentObject(HabitatViewModel())
        .environmentObject(ProgressionViewModel())
}
