import SwiftUI

// MARK: - Habitat Type Picker
// Shown when the player adds a new habitat slot.
// Displays all 7 types; Magical is locked until Story Act II.

struct HabitatTypePickerView: View {
    @EnvironmentObject var habitatVM:  HabitatViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel
    @Environment(\.dismiss) private var dismiss

    @State private var selectedType: HabitatType = .water

    private var expansionCost: Int {
        HabitatExpansionCost.cost(forSlot: habitatVM.habitats.count + 1)
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                costBanner
                typeGrid
                confirmButton
            }
            .background(Color(hex: "#F5F0FF"))
            .navigationTitle("Add Habitat")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
    }

    // MARK: Cost Banner

    private var costBanner: some View {
        HStack(spacing: 10) {
            Image(systemName: "circle.fill")
                .foregroundColor(Color(hex: "#FFD700"))
            Text("Cost: \(expansionCost) Coins")
                .font(.system(size: 15, weight: .semibold, design: .rounded))
            Spacer()
            Text("Balance: \(progressVM.coins)")
                .font(.system(size: 13, design: .rounded))
                .foregroundColor(progressVM.canAffordCoins(expansionCost) ? .secondary : .red)
        }
        .padding(.horizontal, 20)
        .padding(.vertical, 14)
        .background(.white.opacity(0.7))
    }

    // MARK: Type Grid

    private var typeGrid: some View {
        ScrollView {
            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 14) {
                ForEach(HabitatType.allCases, id: \.self) { type in
                    let isLocked = isTypeLocked(type)
                    HabitatTypeCard(
                        type:       type,
                        isSelected: selectedType == type,
                        isLocked:   isLocked
                    ) {
                        if !isLocked { selectedType = type }
                    }
                }
            }
            .padding(16)
        }
    }

    // MARK: Confirm Button

    private var confirmButton: some View {
        VStack(spacing: 0) {
            Divider()
            Button {
                addHabitat()
            } label: {
                HStack(spacing: 8) {
                    Image(systemName: "plus.circle.fill")
                    Text("Add \(selectedType.rawValue) Habitat")
                        .font(.system(size: 16, weight: .bold, design: .rounded))
                }
                .frame(maxWidth: .infinity)
                .padding(.vertical, 16)
                .background(progressVM.canAffordCoins(expansionCost) ? Color(hex: "#C99BFF") : Color.gray.opacity(0.4))
                .foregroundColor(.white)
                .clipShape(RoundedRectangle(cornerRadius: 16))
                .padding(.horizontal, 20)
                .padding(.vertical, 14)
            }
            .disabled(!progressVM.canAffordCoins(expansionCost))
        }
        .background(.white.opacity(0.9))
    }

    // MARK: Helpers

    private func isTypeLocked(_ type: HabitatType) -> Bool {
        guard let requiredAct = type.requiresStoryAct else { return false }
        return progressVM.storyAct < requiredAct
    }

    private func addHabitat() {
        let cost = expansionCost
        guard progressVM.spendCoins(cost) else { return }
        DataManager.shared.addHabitat(type: selectedType, unlockedAtLevel: progressVM.level)
        habitatVM.refresh()
        dismiss()
    }
}

// MARK: - Habitat Type Card

struct HabitatTypeCard: View {
    let type:       HabitatType
    let isSelected: Bool
    let isLocked:   Bool
    let action:     () -> Void

    var body: some View {
        Button(action: action) {
            VStack(spacing: 12) {
                ZStack {
                    Circle()
                        .fill(
                            isLocked
                            ? Color.gray.opacity(0.15)
                            : Color(hex: type.displayColor).opacity(isSelected ? 0.8 : 0.3)
                        )
                        .frame(width: 64, height: 64)

                    if isLocked {
                        Image(systemName: "lock.fill")
                            .font(.system(size: 24))
                            .foregroundColor(.gray.opacity(0.5))
                    } else {
                        Text(habitatEmoji)
                            .font(.system(size: 30))
                    }
                }

                Text(type.rawValue)
                    .font(.system(size: 14, weight: .bold, design: .rounded))
                    .foregroundColor(isLocked ? .secondary : .primary)

                if isLocked {
                    Text("Act II required")
                        .font(.system(size: 10, design: .rounded))
                        .foregroundColor(.secondary)
                } else {
                    Text(typeDescription)
                        .font(.system(size: 11))
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                        .lineLimit(2)
                }
            }
            .padding(16)
            .frame(maxWidth: .infinity)
            .background(isSelected ? Color(hex: type.displayColor).opacity(0.12) : Color.white)
            .clipShape(RoundedRectangle(cornerRadius: 16))
            .overlay(
                RoundedRectangle(cornerRadius: 16)
                    .stroke(
                        isSelected ? Color(hex: type.displayColor) : Color.clear,
                        lineWidth: 2
                    )
            )
            .shadow(color: .black.opacity(0.05), radius: 4, x: 0, y: 2)
            .opacity(isLocked ? 0.5 : 1.0)
        }
        .disabled(isLocked)
    }

    private var habitatEmoji: String {
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

    private var typeDescription: String {
        switch type {
        case .water:    return "15 species available"
        case .dirt:     return "15 species available"
        case .grass:    return "15 species available"
        case .fire:     return "10 species available"
        case .ice:      return "10 species available"
        case .electric: return "10 species available"
        case .magical:  return "5 rare species"
        }
    }
}

// MARK: - Preview

#Preview {
    HabitatTypePickerView()
        .environmentObject(HabitatViewModel())
        .environmentObject(ProgressionViewModel())
}
