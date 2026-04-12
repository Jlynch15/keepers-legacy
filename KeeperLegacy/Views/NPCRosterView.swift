import SwiftUI

// MARK: - NPC Roster View
// Shows all 5 main NPCs, their relationship levels, and story role.
// Accessible from Settings → Characters.

struct NPCRosterView: View {
    @EnvironmentObject var storyVM: StoryViewModel

    var body: some View {
        List {
            Section {
                actProgressHeader
            }
            .listRowBackground(Color.clear)
            .listRowInsets(EdgeInsets())

            Section("Characters") {
                ForEach(storyVM.npcsWithRelationships, id: \.npc.id) { npc, level in
                    NPCRow(npc: npc, relationshipLevel: level)
                }
            }

            Section("Story Progress") {
                storyProgressRow
            }
        }
        .navigationTitle("Characters")
        .navigationBarTitleDisplayMode(.large)
        .background(Color(hex: "#F5F0FF"))
        .scrollContentBackground(.hidden)
    }

    // MARK: Act Progress Header

    private var actProgressHeader: some View {
        VStack(spacing: 12) {
            HStack(spacing: 16) {
                ForEach(StoryAct.allCases, id: \.rawValue) { act in
                    actBadge(act: act, isCurrent: act.rawValue == storyVM.storyState.currentAct,
                             isComplete: act.rawValue < storyVM.storyState.currentAct)
                }
            }
            .padding(.horizontal, 4)
        }
        .padding(.vertical, 12)
        .padding(.horizontal, 16)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 16))
        .padding(.horizontal, 16)
        .padding(.bottom, 4)
    }

    private func actBadge(act: StoryAct, isCurrent: Bool, isComplete: Bool) -> some View {
        VStack(spacing: 6) {
            ZStack {
                Circle()
                    .fill(isComplete ? Color(hex: "#7FFFD4") :
                          isCurrent  ? Color(hex: "#C99BFF") :
                                       Color.secondary.opacity(0.2))
                    .frame(width: 40, height: 40)

                if isComplete {
                    Image(systemName: "checkmark")
                        .font(.system(size: 16, weight: .bold))
                        .foregroundColor(.white)
                } else {
                    Text("I\(String(repeating: "I", count: act.rawValue - 1))")
                        .font(.system(size: 13, weight: .bold, design: .rounded))
                        .foregroundColor(isCurrent ? .white : .secondary)
                }
            }
            Text(actShortTitle(act))
                .font(.system(size: 10, weight: isCurrent ? .bold : .regular, design: .rounded))
                .foregroundColor(isCurrent ? .primary : .secondary)
                .multilineTextAlignment(.center)
        }
        .frame(maxWidth: .infinity)
    }

    // MARK: Story Progress Row

    private var storyProgressRow: some View {
        HStack {
            VStack(alignment: .leading, spacing: 4) {
                Text("Events Discovered")
                    .font(.system(size: 14, design: .rounded))
                Text("\(storyVM.completedEventsCount) of \(storyVM.totalEventsCount)")
                    .font(.system(size: 13, weight: .bold, design: .rounded))
                    .foregroundColor(Color(hex: "#C99BFF"))
            }
            Spacer()
            CircularProgressView(
                fraction: storyVM.totalEventsCount > 0
                    ? Double(storyVM.completedEventsCount) / Double(storyVM.totalEventsCount)
                    : 0,
                color: Color(hex: "#C99BFF"),
                size: 44
            )
        }
        .padding(.vertical, 4)
    }

    private func actShortTitle(_ act: StoryAct) -> String {
        switch act {
        case .act1: return "Discovery"
        case .act2: return "Restoration"
        case .act3: return "Legacy"
        }
    }
}

// MARK: - NPC Row

struct NPCRow: View {
    let npc: NPC
    let relationshipLevel: Int

    var body: some View {
        HStack(spacing: 14) {
            // Avatar
            ZStack {
                Circle()
                    .fill(archetypeColor.opacity(0.2))
                    .frame(width: 52, height: 52)
                Text(npcEmoji)
                    .font(.system(size: 28))
            }

            // Info
            VStack(alignment: .leading, spacing: 6) {
                HStack(spacing: 8) {
                    Text(npc.name)
                        .font(.system(size: 15, weight: .bold, design: .rounded))
                    Text(archetypeLabel)
                        .font(.system(size: 10, weight: .semibold, design: .rounded))
                        .foregroundColor(archetypeColor)
                        .padding(.horizontal, 7)
                        .padding(.vertical, 3)
                        .background(archetypeColor.opacity(0.12))
                        .clipShape(Capsule())
                }

                // Relationship bar
                HStack(spacing: 8) {
                    GeometryReader { geo in
                        ZStack(alignment: .leading) {
                            Capsule().fill(archetypeColor.opacity(0.15))
                            Capsule()
                                .fill(archetypeColor)
                                .frame(width: geo.size.width * CGFloat(relationshipLevel) / 100)
                        }
                    }
                    .frame(height: 6)

                    Text(relationshipTier)
                        .font(.system(size: 10, design: .rounded))
                        .foregroundColor(.secondary)
                        .frame(width: 72, alignment: .trailing)
                }
            }
        }
        .padding(.vertical, 4)
    }

    private var npcEmoji: String {
        switch npc.archetype {
        case .mentor:          return "🧙‍♀️"
        case .businessPartner: return "🧑‍💼"
        case .collector:       return "🌿"
        case .scholar:         return "📚"
        case .rival:           return "⚔️"
        }
    }

    private var archetypeLabel: String {
        switch npc.archetype {
        case .mentor:          return "Mentor"
        case .businessPartner: return "Trader"
        case .collector:       return "Collector"
        case .scholar:         return "Scholar"
        case .rival:           return "Rival"
        }
    }

    private var archetypeColor: Color {
        switch npc.archetype {
        case .mentor:          return Color(hex: "#C99BFF")
        case .businessPartner: return Color(hex: "#FFD700")
        case .collector:       return Color(hex: "#7FFFD4")
        case .scholar:         return Color(hex: "#A8D8EA")
        case .rival:           return Color(hex: "#FF9AA2")
        }
    }

    private var relationshipTier: String {
        switch relationshipLevel {
        case 0..<20:    return "Stranger"
        case 20..<40:   return "Acquaintance"
        case 40..<60:   return "Friend"
        case 60..<80:   return "Trusted"
        default:        return "Bonded"
        }
    }
}

// MARK: - Circular Progress View

struct CircularProgressView: View {
    let fraction: Double
    let color: Color
    let size: CGFloat

    var body: some View {
        ZStack {
            Circle()
                .stroke(color.opacity(0.15), lineWidth: 4)
            Circle()
                .trim(from: 0, to: CGFloat(fraction))
                .stroke(color, style: StrokeStyle(lineWidth: 4, lineCap: .round))
                .rotationEffect(.degrees(-90))
            Text("\(Int(fraction * 100))%")
                .font(.system(size: size * 0.22, weight: .bold, design: .rounded))
                .foregroundColor(color)
        }
        .frame(width: size, height: size)
    }
}

// MARK: - Preview

#Preview {
    NavigationStack {
        NPCRosterView()
            .environmentObject(StoryViewModel())
    }
}
