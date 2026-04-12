import SwiftUI

// MARK: - Breeding View
// Two-step flow: pick Parent B → preview → confirm.
// Parent A is the creature the player tapped "Breed" on in HabitatView.
// Unlocks at Level 12 + Story Act I completion.

struct BreedingView: View {
    @EnvironmentObject var creatureVM:  CreatureViewModel
    @EnvironmentObject var progressVM:  ProgressionViewModel
    @EnvironmentObject var habitatVM:   HabitatViewModel
    @Environment(\.dismiss) private var dismiss

    let parentA: CreatureEntity

    @State private var selectedParentB: CreatureEntity? = nil
    @State private var step: Step = .pickParent

    enum Step { case pickParent, preview, result }

    private var parentAEntry: CreatureCatalogEntry? {
        CreatureCatalogEntry.find(byID: parentA.catalogID ?? "")
    }

    // Compatible creatures: same habitat type, adult, different habitat slot, not parentA
    private var compatibleCreatures: [CreatureEntity] {
        let allOwned = DataManager.shared.allOwnedCreatures()
        return allOwned.filter { creature in
            guard creature.id != parentA.id else { return false }
            guard creature.lifecycle == LifecycleStage.adult.rawValue else { return false }
            guard let catalogID = creature.catalogID,
                  let entry = CreatureCatalogEntry.find(byID: catalogID) else { return false }
            return entry.habitatType == parentAEntry?.habitatType
        }
    }

    var body: some View {
        NavigationStack {
            Group {
                switch step {
                case .pickParent: pickParentBView
                case .preview:    breedingPreviewView
                case .result:     breedingResultView
                }
            }
            .navigationTitle("Breeding")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
    }

    // MARK: Step 1 — Pick Parent B

    private var pickParentBView: some View {
        VStack(spacing: 0) {
            // Parent A display
            parentSummaryHeader

            Divider()

            if compatibleCreatures.isEmpty {
                noCompatibleCreaturesView
            } else {
                ScrollView {
                    VStack(alignment: .leading, spacing: 12) {
                        Text("Choose a mate")
                            .font(.system(size: 14, weight: .semibold, design: .rounded))
                            .foregroundColor(.secondary)
                            .padding(.horizontal)
                            .padding(.top, 16)

                        Text("Must be the same habitat type and fully grown.")
                            .font(.system(size: 12))
                            .foregroundColor(.secondary)
                            .padding(.horizontal)

                        ForEach(compatibleCreatures, id: \.id) { creature in
                            ParentSelectionRow(
                                creature: creature,
                                isSelected: selectedParentB?.id == creature.id
                            ) {
                                selectedParentB = creature
                            }
                        }
                    }
                    .padding(.bottom, 100)
                }

                // Continue button
                VStack {
                    Spacer()
                    Button {
                        step = .preview
                    } label: {
                        Text("Preview Breeding")
                            .font(.system(size: 16, weight: .bold, design: .rounded))
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 16)
                            .background(selectedParentB != nil ? Color(hex: "#C99BFF") : Color.gray.opacity(0.3))
                            .foregroundColor(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 16))
                            .padding(.horizontal, 20)
                            .padding(.bottom, 20)
                    }
                    .disabled(selectedParentB == nil)
                }
                .frame(maxHeight: .infinity, alignment: .bottom)
            }
        }
        .background(Color(hex: "#FFF5FF"))
    }

    // MARK: Step 2 — Breeding Preview

    private var breedingPreviewView: some View {
        ScrollView {
            VStack(spacing: 24) {
                // Parents side by side
                HStack(spacing: 0) {
                    parentCard(creature: parentA, label: "Parent A")
                    Image(systemName: "heart.fill")
                        .foregroundColor(Color(hex: "#C99BFF"))
                        .font(.system(size: 24))
                        .padding(.horizontal, 8)
                    if let b = selectedParentB {
                        parentCard(creature: b, label: "Parent B")
                    }
                }
                .padding(.top, 20)

                // Cost
                costCard

                // Offspring possibilities
                offspringPossibilitiesCard

                // Mutation probabilities
                mutationProbabilitiesCard

                // Confirm button
                Button {
                    attemptBreeding()
                } label: {
                    HStack(spacing: 8) {
                        Image(systemName: "circle.fill")
                            .foregroundColor(Color(hex: "#FFD700"))
                        Text("Breed for \(breedingCost) Coins")
                            .font(.system(size: 16, weight: .bold, design: .rounded))
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 16)
                    .background(progressVM.canAffordCoins(breedingCost) ? Color(hex: "#C99BFF") : Color.gray.opacity(0.3))
                    .foregroundColor(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 16))
                }
                .disabled(!progressVM.canAffordCoins(breedingCost))
                .padding(.horizontal, 20)

                if !progressVM.canAffordCoins(breedingCost) {
                    Text("You need \(breedingCost - progressVM.coins) more Coins.")
                        .font(.system(size: 13))
                        .foregroundColor(.secondary)
                }
            }
            .padding(.bottom, 40)
        }
        .background(Color(hex: "#FFF5FF"))
    }

    // MARK: Step 3 — Result

    private var breedingResultView: some View {
        VStack(spacing: 28) {
            Spacer()

            switch creatureVM.breedingResult {
            case .success(let offspringCatalogID, let mutationIndex):
                successResultView(catalogID: offspringCatalogID, mutation: mutationIndex)
            case .failure(let reason):
                failureResultView(reason: reason)
            case nil:
                ProgressView()
            }

            Spacer()

            Button("Done") { dismiss() }
                .font(.system(size: 16, weight: .bold, design: .rounded))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 16)
                .background(Color(hex: "#C99BFF"))
                .foregroundColor(.white)
                .clipShape(RoundedRectangle(cornerRadius: 16))
                .padding(.horizontal, 20)
                .padding(.bottom, 20)
        }
        .background(Color(hex: "#FFF5FF"))
    }

    // MARK: Sub-Views

    private var parentSummaryHeader: some View {
        HStack(spacing: 14) {
            creaturePortraitCircle(creature: parentA, size: 56)
            VStack(alignment: .leading, spacing: 4) {
                Text("Breeding with")
                    .font(.system(size: 12, design: .rounded))
                    .foregroundColor(.secondary)
                Text(parentAEntry?.name ?? "Unknown")
                    .font(.system(size: 18, weight: .bold, design: .rounded))
                if let entry = parentAEntry {
                    RarityBadge(rarity: entry.rarity)
                }
            }
            Spacer()
        }
        .padding(16)
        .background(.white)
    }

    private func parentCard(creature: CreatureEntity, label: String) -> some View {
        VStack(spacing: 8) {
            Text(label)
                .font(.system(size: 11, weight: .semibold, design: .rounded))
                .foregroundColor(.secondary)
            creaturePortraitCircle(creature: creature, size: 72)
            if let entry = CreatureCatalogEntry.find(byID: creature.catalogID ?? "") {
                Text(entry.name)
                    .font(.system(size: 13, weight: .bold, design: .rounded))
                    .lineLimit(1)
                RarityBadge(rarity: entry.rarity)
            }
        }
        .frame(maxWidth: .infinity)
    }

    private var costCard: some View {
        HStack(spacing: 12) {
            Image(systemName: "circle.fill")
                .foregroundColor(Color(hex: "#FFD700"))
                .font(.system(size: 22))
            VStack(alignment: .leading, spacing: 2) {
                Text("Breeding Cost")
                    .font(.system(size: 12, design: .rounded))
                    .foregroundColor(.secondary)
                Text("\(breedingCost) Coins")
                    .font(.system(size: 20, weight: .bold, design: .rounded))
            }
            Spacer()
            VStack(alignment: .trailing, spacing: 2) {
                Text("Your balance")
                    .font(.system(size: 11, design: .rounded))
                    .foregroundColor(.secondary)
                Text("\(progressVM.coins)")
                    .font(.system(size: 15, weight: .semibold, design: .rounded))
                    .foregroundColor(progressVM.canAffordCoins(breedingCost) ? .green : .red)
            }
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 14))
        .padding(.horizontal, 20)
    }

    private var offspringPossibilitiesCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Possible Offspring")
                .font(.system(size: 14, weight: .semibold, design: .rounded))

            let possibleSpecies = possibleOffspringSpecies
            ForEach(possibleSpecies, id: \.id) { entry in
                HStack(spacing: 10) {
                    Circle()
                        .fill(Color(hex: entry.habitatType.displayColor).opacity(0.4))
                        .frame(width: 36, height: 36)
                        .overlay(
                            Text(habitatEmoji(entry.habitatType))
                                .font(.system(size: 18))
                        )
                    VStack(alignment: .leading, spacing: 2) {
                        Text(entry.name)
                            .font(.system(size: 14, weight: .semibold, design: .rounded))
                        Text(entry.description)
                            .font(.system(size: 11))
                            .foregroundColor(.secondary)
                            .lineLimit(2)
                    }
                    Spacer()
                    Text("50%")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                        .foregroundColor(Color(hex: "#C99BFF"))
                }
            }
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 14))
        .padding(.horizontal, 20)
    }

    private var mutationProbabilitiesCard: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Mutation Chances")
                .font(.system(size: 14, weight: .semibold, design: .rounded))

            let probabilities: [(String, Double, Color)] = [
                ("Variant 1 (Common)",  0.40, Color(hex: "#A8D5A8")),
                ("Variant 2",           0.30, Color(hex: "#A8D8EA")),
                ("Variant 3",           0.20, Color(hex: "#C99BFF")),
                ("Variant 4 (Rare)",    0.10, Color(hex: "#FFD700")),
            ]

            ForEach(probabilities, id: \.0) { label, probability, color in
                HStack(spacing: 10) {
                    Text(label)
                        .font(.system(size: 12, design: .rounded))
                        .frame(width: 130, alignment: .leading)
                    GeometryReader { geo in
                        ZStack(alignment: .leading) {
                            RoundedRectangle(cornerRadius: 4)
                                .fill(color.opacity(0.2))
                            RoundedRectangle(cornerRadius: 4)
                                .fill(color)
                                .frame(width: geo.size.width * CGFloat(probability))
                        }
                    }
                    .frame(height: 8)
                    Text("\(Int(probability * 100))%")
                        .font(.system(size: 11, weight: .bold, design: .monospaced))
                        .foregroundColor(.secondary)
                        .frame(width: 32, alignment: .trailing)
                }
            }
        }
        .padding(16)
        .background(.white)
        .clipShape(RoundedRectangle(cornerRadius: 14))
        .padding(.horizontal, 20)
    }

    private func successResultView(catalogID: String, mutation: Int) -> some View {
        let entry = CreatureCatalogEntry.find(byID: catalogID)
        return VStack(spacing: 20) {
            Image(systemName: "sparkles")
                .font(.system(size: 64))
                .foregroundStyle(
                    LinearGradient(
                        colors: [Color(hex: "#FFD700"), Color(hex: "#C99BFF")],
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    )
                )
            Text("A new egg appeared!")
                .font(.system(size: 26, weight: .bold, design: .rounded))
            if let entry = entry {
                VStack(spacing: 8) {
                    Text(entry.name)
                        .font(.system(size: 18, weight: .semibold, design: .rounded))
                    RarityBadge(rarity: entry.rarity)
                    if mutation < entry.mutations.count {
                        Text("Variant: \(entry.mutations[mutation].colorHint)")
                            .font(.system(size: 14))
                            .foregroundColor(.secondary)
                    }
                }
                .padding(20)
                .background(.white)
                .clipShape(RoundedRectangle(cornerRadius: 16))
                .padding(.horizontal, 40)
            }
            Text("The egg will hatch in your habitat.")
                .font(.system(size: 14))
                .foregroundColor(.secondary)
        }
    }

    private func failureResultView(reason: String) -> some View {
        VStack(spacing: 16) {
            Image(systemName: "xmark.circle")
                .font(.system(size: 64))
                .foregroundColor(.secondary)
            Text("Breeding Failed")
                .font(.system(size: 22, weight: .bold, design: .rounded))
            Text(reason)
                .font(.system(size: 15))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 40)
        }
    }

    private var noCompatibleCreaturesView: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "heart.slash")
                .font(.system(size: 56))
                .foregroundColor(Color(hex: "#C99BFF").opacity(0.4))
            Text("No compatible mates")
                .font(.system(size: 18, weight: .bold, design: .rounded))
            Text("You need another adult \(parentAEntry?.habitatType.rawValue ?? "") creature to breed. Visit the shop to find one!")
                .font(.system(size: 14))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal, 40)
            Spacer()
        }
    }

    // MARK: Helpers

    private var breedingCost: Int {
        PricingTable.Breeding.cost(rarity: parentAEntry?.rarity ?? .common)
    }

    private var possibleOffspringSpecies: [CreatureCatalogEntry] {
        var species: [CreatureCatalogEntry] = []
        if let a = parentAEntry { species.append(a) }
        if let b = selectedParentB,
           let entry = CreatureCatalogEntry.find(byID: b.catalogID ?? ""),
           entry.id != parentAEntry?.id {
            species.append(entry)
        }
        return species
    }

    private func attemptBreeding() {
        guard let parentB = selectedParentB else { return }
        creatureVM.breed(parentA: parentA, parentB: parentB, progressVM: progressVM)
        habitatVM.refresh()
        step = .result
    }

    private func creaturePortraitCircle(creature: CreatureEntity, size: CGFloat) -> some View {
        let entry = CreatureCatalogEntry.find(byID: creature.catalogID ?? "")
        return Circle()
            .fill(Color(hex: entry?.habitatType.displayColor ?? "#C99BFF").opacity(0.35))
            .frame(width: size, height: size)
            .overlay(
                Text(habitatEmoji(entry?.habitatType ?? .water))
                    .font(.system(size: size * 0.45))
            )
    }

    private func habitatEmoji(_ type: HabitatType) -> String {
        switch type {
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

// MARK: - Parent Selection Row

struct ParentSelectionRow: View {
    let creature:   CreatureEntity
    let isSelected: Bool
    let action:     () -> Void

    private var entry: CreatureCatalogEntry? {
        CreatureCatalogEntry.find(byID: creature.catalogID ?? "")
    }

    var body: some View {
        Button(action: action) {
            HStack(spacing: 14) {
                // Selection indicator
                ZStack {
                    Circle()
                        .stroke(isSelected ? Color(hex: "#C99BFF") : Color.gray.opacity(0.3), lineWidth: 2)
                        .frame(width: 24, height: 24)
                    if isSelected {
                        Circle()
                            .fill(Color(hex: "#C99BFF"))
                            .frame(width: 14, height: 14)
                    }
                }

                // Portrait
                Circle()
                    .fill(Color(hex: entry?.habitatType.displayColor ?? "#C99BFF").opacity(0.3))
                    .frame(width: 48, height: 48)
                    .overlay(
                        Text(habitatEmoji(entry?.habitatType ?? .water))
                            .font(.system(size: 22))
                    )

                // Info
                VStack(alignment: .leading, spacing: 4) {
                    Text(entry?.name ?? "Unknown")
                        .font(.system(size: 14, weight: .bold, design: .rounded))
                    HStack(spacing: 6) {
                        if let entry = entry {
                            RarityBadge(rarity: entry.rarity)
                        }
                        // Happiness indicator
                        HStack(spacing: 3) {
                            Image(systemName: "face.smiling")
                                .font(.system(size: 10))
                            Text("\(Int(creature.happiness * 100))%")
                                .font(.system(size: 11, design: .rounded))
                        }
                        .foregroundColor(.secondary)
                    }
                }

                Spacer()

                // Mutation badge
                Text("V\(creature.mutationIndex + 1)")
                    .font(.system(size: 11, weight: .bold, design: .rounded))
                    .foregroundColor(.white)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(Color(hex: "#C99BFF").opacity(0.7))
                    .clipShape(Capsule())
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 10)
            .background(isSelected ? Color(hex: "#C99BFF").opacity(0.08) : Color.white)
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(isSelected ? Color(hex: "#C99BFF") : Color.clear, lineWidth: 1.5)
            )
            .padding(.horizontal, 16)
        }
    }

    private func habitatEmoji(_ type: HabitatType) -> String {
        switch type {
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

// MARK: - Preview

#Preview {
    let dm = DataManager.preview
    let creature = dm.allOwnedCreatures().first!
    return BreedingView(parentA: creature)
        .environmentObject(CreatureViewModel())
        .environmentObject(ProgressionViewModel())
        .environmentObject(HabitatViewModel())
}
