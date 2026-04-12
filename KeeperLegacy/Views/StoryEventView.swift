import SwiftUI

// MARK: - Story Event View
// Full-screen overlay shown when a story event triggers.
// Dismissed by tapping "Continue", which calls storyVM.completeCurrentEvent().

struct StoryEventView: View {
    @EnvironmentObject var storyVM:   StoryViewModel
    @EnvironmentObject var progressVM: ProgressionViewModel

    let event: StoryEvent

    @State private var scale:   CGFloat = 0.92
    @State private var opacity: Double  = 0

    private var npc: NPC { storyVM.npc(for: event.npcID) }

    var body: some View {
        ZStack {
            // Dim backdrop
            Color.black.opacity(0.6)
                .ignoresSafeArea()

            // Card
            ScrollView {
                VStack(spacing: 0) {
                    actBanner
                    cardBody
                }
                .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 28))
                .padding(.horizontal, 24)
                .padding(.vertical, 60)
            }
            .scaleEffect(scale)
            .opacity(opacity)
        }
        .onAppear {
            withAnimation(.spring(response: 0.4, dampingFraction: 0.7)) {
                scale   = 1
                opacity = 1
            }
        }
    }

    // MARK: Act Banner

    private var actBanner: some View {
        HStack(spacing: 8) {
            Image(systemName: "book.pages.fill")
                .font(.system(size: 13))
                .foregroundColor(actColor)
            Text(event.act.title.uppercased())
                .font(.system(size: 11, weight: .bold, design: .rounded))
                .foregroundColor(actColor)
                .tracking(1.5)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 10)
        .frame(maxWidth: .infinity)
        .background(actColor.opacity(0.15))
        .clipShape(UnevenRoundedRectangle(topLeadingRadius: 28, bottomLeadingRadius: 0,
                                          bottomTrailingRadius: 0, topTrailingRadius: 28))
    }

    // MARK: Card Body

    private var cardBody: some View {
        VStack(spacing: 24) {

            // NPC portrait
            npcPortrait

            // Title
            Text(event.title)
                .font(.system(size: 24, weight: .black, design: .rounded))
                .multilineTextAlignment(.center)

            // Dialogue body
            Text(event.body)
                .font(.system(size: 15, design: .rounded))
                .foregroundColor(.secondary)
                .multilineTextAlignment(.leading)
                .lineSpacing(4)
                .frame(maxWidth: .infinity, alignment: .leading)

            // Feature unlock callout
            if let feature = event.unlocksFeature {
                featureUnlockCallout(feature: feature)
            }

            // Act advancement callout
            if event.advancesToNextAct {
                actAdvancementCallout
            }

            // Continue button
            Button {
                withAnimation(.easeIn(duration: 0.2)) {
                    opacity = 0
                    scale   = 0.95
                }
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.2) {
                    storyVM.completeCurrentEvent(progressVM: progressVM)
                }
            } label: {
                Text("Continue")
                    .font(.system(size: 17, weight: .bold, design: .rounded))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 16)
                    .background(actColor)
                    .foregroundColor(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 16))
            }
            .padding(.top, 4)
        }
        .padding(24)
    }

    // MARK: NPC Portrait

    private var npcPortrait: some View {
        VStack(spacing: 10) {
            ZStack {
                Circle()
                    .fill(
                        LinearGradient(
                            colors: [actColor.opacity(0.3), actColor.opacity(0.6)],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )
                    .frame(width: 88, height: 88)
                    .shadow(color: actColor.opacity(0.4), radius: 12)

                Text(npcEmoji(npc))
                    .font(.system(size: 46))
            }

            Text(npc.name)
                .font(.system(size: 14, weight: .bold, design: .rounded))
                .foregroundColor(.primary)

            Text(archetypeLabel(npc.archetype))
                .font(.system(size: 11, design: .rounded))
                .foregroundColor(.secondary)
                .padding(.horizontal, 10)
                .padding(.vertical, 4)
                .background(Color.secondary.opacity(0.1))
                .clipShape(Capsule())
        }
    }

    // MARK: Feature Unlock Callout

    private func featureUnlockCallout(feature: GameFeature) -> some View {
        HStack(spacing: 12) {
            Image(systemName: "lock.open.fill")
                .font(.system(size: 20))
                .foregroundColor(Color(hex: "#7FFFD4"))
            VStack(alignment: .leading, spacing: 2) {
                Text("New Feature Unlocked")
                    .font(.system(size: 11, weight: .semibold, design: .rounded))
                    .foregroundColor(.secondary)
                    .textCase(.uppercase)
                    .tracking(0.5)
                Text(featureDisplayName(feature))
                    .font(.system(size: 15, weight: .bold, design: .rounded))
            }
            Spacer()
        }
        .padding(14)
        .background(Color(hex: "#7FFFD4").opacity(0.12))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: Act Advancement Callout

    private var actAdvancementCallout: some View {
        let nextActNum = min(storyVM.storyState.currentAct + 1, 3)
        let nextAct    = StoryAct(rawValue: nextActNum) ?? .act2
        return HStack(spacing: 12) {
            Image(systemName: "book.pages.fill")
                .font(.system(size: 20))
                .foregroundColor(Color(hex: "#C99BFF"))
            VStack(alignment: .leading, spacing: 2) {
                Text("Story Advancing")
                    .font(.system(size: 11, weight: .semibold, design: .rounded))
                    .foregroundColor(.secondary)
                    .textCase(.uppercase)
                    .tracking(0.5)
                Text(nextAct.title)
                    .font(.system(size: 15, weight: .bold, design: .rounded))
            }
            Spacer()
        }
        .padding(14)
        .background(Color(hex: "#C99BFF").opacity(0.12))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // MARK: Helpers

    private var actColor: Color {
        switch event.act {
        case .act1: return Color(hex: "#7FBFEA")
        case .act2: return Color(hex: "#C99BFF")
        case .act3: return Color(hex: "#FFD700")
        }
    }

    private func npcEmoji(_ npc: NPC) -> String {
        switch npc.archetype {
        case .mentor:          return "🧙‍♀️"
        case .businessPartner: return "🧑‍💼"
        case .collector:       return "🌿"
        case .scholar:         return "📚"
        case .rival:           return "⚔️"
        }
    }

    private func archetypeLabel(_ archetype: NPC.Archetype) -> String {
        switch archetype {
        case .mentor:          return "Mentor"
        case .businessPartner: return "Trader"
        case .collector:       return "Collector"
        case .scholar:         return "Scholar"
        case .rival:           return "Rival"
        }
    }

    private func featureDisplayName(_ feature: GameFeature) -> String {
        switch feature {
        case .breeding:         return "Breeding"
        case .mutations:        return "Mutation Variants"
        case .monsterpedia:     return "Monsterpedia"
        case .habitatExpansion: return "Habitat Expansion"
        case .cosmetics:        return "Cosmetics Shop"
        case .magicalHabitat:   return "Magical Habitat"
        case .customerOrders:   return "Customer Orders"
        default:                return feature.rawValue.capitalized
        }
    }
}

// MARK: - Preview

#Preview("Act I — First Egg") {
    StoryEventView(event: StoryEvent.allEvents[0])
        .environmentObject(StoryViewModel())
        .environmentObject(ProgressionViewModel())
}

#Preview("Act I — Magic Revealed (feature unlock + act advance)") {
    StoryEventView(event: StoryEvent.allEvents[2])
        .environmentObject(StoryViewModel())
        .environmentObject(ProgressionViewModel())
}

#Preview("Act II — Restoration (magical habitat)") {
    StoryEventView(event: StoryEvent.allEvents[4])
        .environmentObject(StoryViewModel())
        .environmentObject(ProgressionViewModel())
}
