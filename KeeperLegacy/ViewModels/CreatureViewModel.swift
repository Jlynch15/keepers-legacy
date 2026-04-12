import Foundation
import Combine

// MARK: - CreatureViewModel
// Manages breeding logic and the lifecycle state of individual creatures.
// Breeding unlocks at Level 12 + Story Act I completion.

@MainActor
final class CreatureViewModel: ObservableObject {
    @Published var breedingResult: BreedingResult? = nil
    @Published var isBreeding: Bool = false

    private let dataManager = DataManager.shared

    enum BreedingResult {
        case success(offspringCatalogID: String, mutationIndex: Int)
        case failure(reason: String)
    }

    // MARK: Breeding

    /// Attempt to breed two creatures. Deducts coins, creates offspring if successful.
    func breed(
        parentA: CreatureEntity,
        parentB: CreatureEntity,
        progressVM: ProgressionViewModel
    ) {
        guard progressVM.isFeatureUnlocked(.breeding) else {
            breedingResult = .failure(reason: "Breeding unlocks at Level 12 after completing Act I.")
            return
        }
        guard let idA = parentA.catalogID, let idB = parentB.catalogID,
              let entryA = CreatureCatalogEntry.find(byID: idA),
              let entryB = CreatureCatalogEntry.find(byID: idB) else {
            breedingResult = .failure(reason: "Invalid creatures selected.")
            return
        }
        guard entryA.habitatType == entryB.habitatType else {
            breedingResult = .failure(reason: "Creatures must be the same habitat type to breed.")
            return
        }
        guard parentA.lifecycle == LifecycleStage.adult.rawValue,
              parentB.lifecycle == LifecycleStage.adult.rawValue else {
            breedingResult = .failure(reason: "Both creatures must be adults to breed.")
            return
        }

        let cost = PricingTable.Breeding.cost(rarity: entryA.rarity)
        guard progressVM.spendCoins(cost) else {
            breedingResult = .failure(reason: "Not enough Coins. Need \(cost) coins.")
            return
        }

        isBreeding = true

        // Determine offspring: 50/50 parent species, random mutation with rarity-weighted inheritance
        let offspringCatalogID = Bool.random() ? entryA.id : entryB.id
        let offspringMutation  = inheritedMutation(entryA: entryA, entryB: entryB)

        let offspring = dataManager.createCreature(
            catalogID:     offspringCatalogID,
            mutationIndex: offspringMutation,
            parentIDs:     [parentA.id!, parentB.id!]
        )

        dataManager.recordBreeding(
            parentAID:   parentA.id!,
            parentBID:   parentB.id!,
            offspringID: offspring.id!
        )

        progressVM.addXP(XPSource.breedSuccess.xpReward)

        isBreeding    = false
        breedingResult = .success(offspringCatalogID: offspringCatalogID, mutationIndex: offspringMutation)

        // Notify story system that a breed occurred (may trigger story events)
        NotificationCenter.default.post(name: .storyCheckNeeded, object: nil,
                                        userInfo: ["breedCount": dataManager.allBreedingRecords().count])
    }

    /// Genetic inheritance: weighted random mutation selection.
    /// Rare mutations (index 3) have lower probability.
    private func inheritedMutation(entryA: CreatureCatalogEntry, entryB: CreatureCatalogEntry) -> Int {
        let weights: [Double] = [0.40, 0.30, 0.20, 0.10]  // mutations 0–3
        let roll = Double.random(in: 0...1)
        var cumulative = 0.0
        for (index, weight) in weights.enumerated() {
            cumulative += weight
            if roll <= cumulative { return index }
        }
        return 0
    }

    // MARK: Lifecycle Progression

    /// Checks if any creatures need to advance their lifecycle stage.
    func checkLifecycleProgressions() {
        let creatures = dataManager.allOwnedCreatures()
        for creature in creatures {
            guard let stageRaw = creature.lifecycle,
                  let stage = LifecycleStage(rawValue: stageRaw),
                  stage != .adult,
                  let startDate = creature.lifecycleStartDate else { continue }

            let hoursPassed = Date().timeIntervalSince(startDate) / 3600
            if hoursPassed >= stage.durationHours {
                advanceLifecycle(creature: creature, currentStage: stage)
            }
        }
    }

    private func advanceLifecycle(creature: CreatureEntity, currentStage: LifecycleStage) {
        let next: LifecycleStage
        switch currentStage {
        case .egg:        next = .baby
        case .baby:       next = .adolescent
        case .adolescent: next = .adult
        case .adult:      return
        }
        creature.lifecycle          = next.rawValue
        creature.lifecycleStartDate = Date()
        dataManager.save()
    }
}
