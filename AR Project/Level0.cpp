// Fill out your copyright notice in the Description page of Project Settings.

#include "Level0.h"

#include "ARPin.h"
#include "ThePlayer.h"
#include "Kismet/GameplayStatics.h"

// Sets default values
ALevel0::ALevel0()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;
	sceneComponent = CreateDefaultSubobject<USceneComponent>(TEXT("SceneComponent"));
	SetRootComponent(sceneComponent);
	sceneComponent->SetMobility(EComponentMobility::Movable);
	
	staticMeshParent = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("MeshParent"));
	staticMeshParent->SetupAttachment(sceneComponent);
	staticMeshParent->SetSimulatePhysics(true);

	static ConstructorHelpers::FObjectFinder<UClass>FoundWidget(TEXT ("Class'/Game/Blueprints/UI/WB_WinScreen.WB_WinScreen_C'"));

	if(FoundWidget.Succeeded()) levelCompleteScreenClass = FoundWidget.Object; 
	
	customGameMode = Cast<ACustomGameMode>(UGameplayStatics::GetGameMode(this));
}

// Called when the game starts or when spawned
void ALevel0::BeginPlay()
{
	Super::BeginPlay();
	
	GetComponents<UStaticMeshComponent>(staticMeshComponents);
	GetComponents<UChildActorComponent>(emptyChildActors);
	//FString num = FString::Printf(TEXT("Total child actors comps: %i"), emptyChildActors.Num());
	
	for(UStaticMeshComponent* meshComp : staticMeshComponents)
	{
		staticMeshHealthMap.Add(meshComp, meshHealth);

		//Enable notify collisions
		meshComp->SetNotifyRigidBodyCollision(true); //Again, very important wee line
	}
	
	// Log the number of static mesh components found
	//GEngine->AddOnScreenDebugMessage(-1, 2.f, FColor::Yellow, num);

	//SpawnTicTacs();
}

void ALevel0::SpawnTicTacs()
{
	for(UChildActorComponent* emptyActor : emptyChildActors)
	{
		if(emptyActor)
		{
			//GEngine->AddOnScreenDebugMessage(-1, 5.f, FColor::Green, FString::Printf(TEXT("Spawning tic tac!!!!!")));
			
			//Spawn the tic tacs from here
			FVector loc = emptyActor->GetComponentLocation();
			FVector worldLoc = emptyActor->GetComponentTransform().TransformPosition(loc);
			
			ATicTac* tictac =  GetWorld()->SpawnActor<ATicTac>(ATicTac::StaticClass(), loc, FRotator::ZeroRotator);

			//Attach the tictac to the static mesh parent for correct positioning
			tictac->AttachToComponent(staticMeshParent, FAttachmentTransformRules::KeepRelativeTransform);

			//Set the position again here
			tictac->SetActorLocation(loc);
			tictac->SetPhysicsSimulation(true);
		}
	}
}


// Called every frame
void ALevel0::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	// Making sure the actor remains on the ARPin that has been found.
	if(PinComponent)
	{
		auto TrackingState = PinComponent->GetTrackingState();
		
		switch (TrackingState)
		{
		case EARTrackingState::Tracking:
			staticMeshParent->SetVisibility(true);
			SetActorTransform(PinComponent->GetLocalToWorldTransform());
	
			// Scale down default cube mesh - Change this for your applications.
			//SetActorScale3D(FVector(0.2f, 0.2f, 0.2f));
			break;
	
		case EARTrackingState::NotTracking:
			PinComponent = nullptr;
	
			break;
		}
	}

	TArray<UStaticMeshComponent*> compsToRemove;
	// Check the speed of static mesh components
	for (const auto& meshEntry : staticMeshHealthMap)
	{
		UStaticMeshComponent* meshComp = meshEntry.Key;

		// Ensure the mesh component is valid and simulating physics
		if (meshComp && meshComp->IsSimulatingPhysics())
		{
			FVector currentVelocity = meshComp->GetComponentVelocity();

			// Check the speed on the Z-axis (you may need to adjust the threshold)
			float speedThreshold = 30.0f;
			if (FMath::Abs(currentVelocity.Z) > speedThreshold)
			{
				// Destroy the static mesh component
				compsToRemove.Add(meshComp);
				staticMeshHealthMap.Remove(meshComp);
			}
		}
	}

	for(UStaticMeshComponent* component : compsToRemove)
	{
		component->DestroyComponent();
	}

}

void ALevel0::ItemDrop()
{
	/*This is where there will be a chance for dropping an ammo object when the health of a
	 * static mesh reaches zero
	 */
	TArray<UStaticMeshComponent*> compsToRemove;
	
	for(auto& meshEntry : staticMeshHealthMap)
	{
		UStaticMeshComponent* meshComp = meshEntry.Key;
		int& health = meshEntry.Value;

		//If the health is below or equal to 0
		if(health <= 0)
		{
			//Add to array for removal after destruction of the component
			compsToRemove.Add(meshComp);
			staticMeshHealthMap.Remove(meshComp);
			
			//If this mesh health is below or equal to 0 then destroy it
			const int dropChance = FMath::RandRange(0, dropRate);
			if(dropChance <= dropRate)
			{
				//Spawn the object here
				// FString num = FString::Printf(TEXT("Total meshes: %i"), staticMeshHealthMap.Num());
				// GEngine->AddOnScreenDebugMessage(-1, 5.f, FColor::Yellow, num);

				FVector location = meshComp->GetComponentLocation();
				FRotator rotation = meshComp->GetComponentRotation();
				
				AThePlayer* playerRef = customGameMode->GetPlayerReference();
				playerRef->SpawnItemDrop(location, rotation);
			}
		}
	}

	for(UStaticMeshComponent* component : compsToRemove)
	{
		component->DestroyComponent();
	}

	//End level here
	//If the player destroys all blocks in a level switch to win screen. Once here, player goes back to level select window.
	if(staticMeshHealthMap.Num() <= 1)
	{
		//Create new widget
		levelCompleteScreen = CreateWidget<UWidgetBase>(GetWorld(), levelCompleteScreenClass);

		levelCompleteScreen->AddToViewport();

		//Set the level is spawned in the player class to false
		AThePlayer* playerRef = customGameMode->GetPlayerReference();
		playerRef->SetLevelSpawned(false);
		playerRef->EnableThrow(false);
		playerRef->ResetScore();

		HelloARManager = customGameMode->GetHelloARManager();
		HelloARManager->EnablePlaneUpdate(false);
		customGameMode->ResetLevel();
		Destroy();
	}
}

void ALevel0::DecrementHealth(UStaticMeshComponent* meshComp, int damage)
{
	if(isPlatform) return;
	//Decrement the health when a projectile hits a static mesh
	for(const auto& entry : staticMeshHealthMap)
	{
		if(entry.Key == meshComp)
		{
			//Decrement the health of the mesh
			int& health = staticMeshHealthMap[meshComp];
			health -= damage;
			// GEngine->AddOnScreenDebugMessage(-1, 2, FColor::Yellow, (TEXT("Decrementing health...")));
			break;
		}
	}
	
	//Check if the health of a static mesh has reached zero
	ItemDrop();
}


/*Setters*/
void ALevel0::SetPhysicsSimulation(bool val)
{
	//Always be false since this is merely the container of the level which helps for scaling
	staticMeshParent->SetSimulatePhysics(false);
	
	//Iterate through all the static mesh components attached to the level
	for(auto& meshEntry : staticMeshHealthMap)
	{
		meshEntry.Key->SetSimulatePhysics(val);
	}
}

//Allows for changing the mobility of an object instance inheriting from this class
void ALevel0::SetObjectMobility(EComponentMobility::Type mobility)
{
	staticMeshParent->SetMobility(mobility);
}

void ALevel0::SetObjectScale(const FVector& scale)
{
	staticMeshParent->SetWorldScale3D(scale);
}

void ALevel0::SetIsPlatform()
{
	isPlatform = true;
}

/*Getters*/
bool ALevel0::GetIsPlatform()
{
	return isPlatform;
}