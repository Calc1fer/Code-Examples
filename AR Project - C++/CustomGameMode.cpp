// Fill out your copyright notice in the Description page of Project Settings.


#include "CustomGameMode.h"
#include "ThePlayer.h"
#include "Level0.h"
#include "CustomGameState.h"
#include "HelloARManager.h"
#include "ARBlueprintLibrary.h"
#include "BombProjectile.h"
#include "Projectile.h"
#include "HeadMountedDisplayFunctionLibrary.h"
#include "Runtime/Engine/Classes/Kismet/GameplayStatics.h"

ACustomGameMode* ACustomGameMode::instance = nullptr;

ACustomGameMode::ACustomGameMode():
	level(nullptr),
	levelPlatform(nullptr),
	regularProjectile(nullptr),
	bombProjectile(nullptr)
{
	// Add this line to your code if you wish to use the Tick() function
	PrimaryActorTick.bCanEverTick = true;
	
	// Set the default pawn and gamestate to be our custom pawn and gamestate programatically
	DefaultPawnClass = AThePlayer::StaticClass();
	GameStateClass = ACustomGameState::StaticClass();

	//Load all Levels here and add them to the levels array
	static ConstructorHelpers::FObjectFinder<UClass>FoundLevel0(TEXT ("Class'/Game/Blueprints/Levels/BP_Level0.BP_Level0_C'"));
	
	static ConstructorHelpers::FObjectFinder<UClass>FoundLevel1(TEXT ("Class'/Game/Blueprints/Levels/BP_Level1.BP_Level1_C'"));

	static ConstructorHelpers::FObjectFinder<UClass>FoundLevel2(TEXT ("Class'/Game/Blueprints/Levels/BP_Level2.BP_Level2_C'"));
	
	//Platform
	static ConstructorHelpers::FObjectFinder<UClass>FoundPlatform(TEXT ("Class'/Game/Blueprints/Levels/BP_Platform.BP_Platform_C'"));

	//Assign the found blueprint classes to the relevant instances
	if(FoundLevel0.Succeeded()) levelInstance = FoundLevel0.Object;
	levels.Add(levelInstance);
	if(FoundLevel1.Succeeded()) levelInstance = FoundLevel1.Object;
	levels.Add(levelInstance);
	if(FoundLevel2.Succeeded()) levelInstance = FoundLevel2.Object;
	levels.Add(levelInstance);
	if(FoundPlatform.Succeeded()) levelPlatformInstance = FoundPlatform.Object; 
}


void ACustomGameMode::StartPlay() 
{
	instance = this;
	SpawnInitialActors();
	
	// This is called before BeginPlay
	StartPlayEvent();

	// This function will transcend to call BeginPlay on all the actors 
	Super::StartPlay();
}

void ACustomGameMode::SetLevelIndex(int val)
{
	levelIndex = val;
}

// An implementation of the StartPlayEvent which can be triggered by calling StartPlayEvent() 
void ACustomGameMode::StartPlayEvent_Implementation() 
{

}

void ACustomGameMode::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

}

void ACustomGameMode::SpawnInitialActors()
{
	if(HelloARManager) return;
	
	// Spawn an instance of the HelloARManager class
	FVector Pos(0,0,0);
	FRotator Rot(0,0,0);
	FActorSpawnParameters SpawnInfo;

	//Spawn the HelloARManager here
	HelloARManager = GetWorld()->SpawnActor<AHelloARManager>(Pos, Rot, SpawnInfo);
	//HelloARManager->EnablePlaneUpdate(true);
}

/*This function will spawn a projectile where the player presses on the screen*/
void ACustomGameMode::SpawnProjectile(FVector screenPos, ProjectileType projectileType)
{
	const APlayerController* playerController = GetPlayerController();
	FVector worldPos, worldDir;

	//Deproject screen position to world space
	UGameplayStatics::DeprojectScreenToWorld(playerController, FVector2D(screenPos), worldPos, worldDir);

	//Set the distance in front of the camera for spawning the projectile
	projectileDistanceOffset = 100.f;
	FVector spawnLocation = worldPos + projectileDistanceOffset * worldDir;
	const FActorSpawnParameters spawnInfo;
	const FRotator rotation(0,0,0);
	
	switch(projectileType)
	{
	case ProjectileType::Regular:
		regularProjectile = GetWorld()->SpawnActor<AProjectile>(spawnLocation, rotation, spawnInfo);
		break;
		
	case ProjectileType::Bomb:
		// GEngine->AddOnScreenDebugMessage(-1, 2.f, FColor::Cyan, (TEXT("We be bomb!!")));
		bombProjectile = GetWorld()->SpawnActor<ABombProjectile>(spawnLocation, rotation, spawnInfo);
		playerRef->SetProjectileType(ProjectileType::Regular);
	}
}

void ACustomGameMode::MoveProjectile(FVector screenPos)
{
	FVector worldPos, worldDir;
	const APlayerController* playerController = GetPlayerController();

	//Deproject to world space
	UGameplayStatics::DeprojectScreenToWorld(playerController, FVector2D(screenPos), worldPos, worldDir);
	
	if(regularProjectile && regularProjectile->IsValidLowLevel())
	{
		//Set the new location of the player projectile
		FVector newPos = worldPos + projectileDistanceOffset * worldDir;
		regularProjectile->SetActorLocation(newPos);
	}

	if(bombProjectile && bombProjectile->IsValidLowLevel())
	{
		//Set the new location of the player projectile
		FVector newPos = worldPos + projectileDistanceOffset * worldDir;
		bombProjectile->SetActorLocation(newPos);
	}
}

/*This function deals with launching the projectile when the player lets go
 * of the screen
 */
void ACustomGameMode::LaunchProjectile(const FVector& startPos, const FVector& endPos, float touchTime)
{
	//Cap the touchTime so player cannot infinitely increase the speed
	if(touchTime > 0.5f) touchTime = 0.5f;
	if (regularProjectile && regularProjectile->IsValidLowLevel())
	{
		//Set the projectile physics to enabled
		regularProjectile->SetPhysicsSimulation(true);

		//Set a new gravitational force for moving the object down
		FVector newGravity = FVector(0.f,0.f,-2.8f);
		
		//Clamp the velocity of the player projectile forward and right motion (X,Y)
		FVector velocityScale = FVector(1.75f,1.75f,0.5f);
		FVector currentVel = regularProjectile->GetStaticMeshComponent()->GetComponentVelocity();
		FVector velocity = currentVel * velocityScale;

		velocity.Z = FMath::Clamp(velocity.Z, 0, 150.f);
		velocity.Y = FMath::Clamp(velocity.Y, -500.f, 500.f);
		velocity.X = FMath::Clamp(velocity.X, -500.f, 500.f);

		regularProjectile->GetStaticMeshComponent()->SetPhysicsLinearVelocity(velocity);
		regularProjectile->SetPlayerProjectile(true); //Set this so player score increments
	}

	if(bombProjectile && bombProjectile->IsValidLowLevel())
	{
		//Set the projectile physics to enabled
		bombProjectile->SetPhysicsSimulation(true);

		//Set a new gravitational force for moving the object down
		FVector newGravity = FVector(0.f,0.f,-2.8f);
		
		//Clamp the velocity of the player projectile forward and right motion (X,Y)
		FVector velocityScale = FVector(1.75f,1.75f,0.5f);
		FVector currentVel = bombProjectile->GetStaticMeshComponent()->GetComponentVelocity();
		FVector velocity = currentVel * velocityScale;

		velocity.Z = FMath::Clamp(velocity.Z, 0, 150.f);
		velocity.Y = FMath::Clamp(velocity.Y, -500.f, 500.f);
		velocity.X = FMath::Clamp(velocity.X, -500.f, 500.f);

		bombProjectile->GetStaticMeshComponent()->SetPhysicsLinearVelocity(velocity);
		bombProjectile->SetPlayerProjectile(true); //Set this so player score increments
	}
}

//This will be the function where the player places the level on a plane
void ACustomGameMode::SpawnLevel(FVector screenPos)
{
	if(HelloARManager->GetPausePlanes()) return;
	const TOptional<FARTraceResult> traceResult = LineTrace(screenPos);

	//If the placing of the level is not on a valid plane then return
	if(!traceResult.IsSet()) return;
	HelloARManager->EnablePlaneUpdate(true);
	playerRef->SetLevelSpawned(true);
	playerRef->EnableThrow(true);	//Player can throw now
	
	bool continueHere;
	const FTransform trackedTF = traceResult.GetValue().GetLocalToWorldTransform();

	if(levelInstance)
	{
			continueHere = true;
	}
	else
	{
		return; // or handle the failure appropriately
	}

	if(!continueHere) return;
	// Logic for both ARPin and non-ARPin cases
	if (!level)
	{
		//Set the location to the hit position from the trace
		const FRotator MyRot(0, 0, 0);
		FVector MyLoc = trackedTF.GetTranslation();
		MyLoc.Z = levelPlatform->GetActorLocation().Z + (levelPlatform->GetActorScale3D().Z) * 10.f; 
		
		level = GetWorld()->SpawnActor<ALevel0>(levels[levelIndex], MyLoc, MyRot);
		FVector scale = FVector(0.15f, 0.15f, 0.15f);
		level->SetObjectMobility(EComponentMobility::Movable);
		level->SetObjectScale(scale);
		level->SpawnTicTacs();
		level->SetPhysicsSimulation(true);
	}

	// Set the spawned actor location based on the Pin.
	level->SetActorTransform(trackedTF);
	level->PinComponent = UARBlueprintLibrary::PinComponent(nullptr, trackedTF, traceResult.GetValue().GetTrackedGeometry());
}

void ACustomGameMode::SpawnPlatform(FVector screenPos)
{
	if(HelloARManager->GetPausePlanes()) return;
	const TOptional<FARTraceResult> traceResult = LineTrace(screenPos);

	//If the placing of the level is not on a valid plane then return
	if(!traceResult.IsSet()) return;
	playerRef->SetPlatformSpawned(true);
	
	bool continueHere;
	const FTransform trackedTF = traceResult.GetValue().GetLocalToWorldTransform();
	
	if(levelPlatformInstance)
	{
		continueHere = true;
	}
	else
	{
		return; // or handle the failure appropriately
	}

	if(!continueHere) return;
	// Logic for both ARPin and non-ARPin cases
	if (!levelPlatform)
	{
		//Set the location to the hit position from the trace
		const FRotator MyRot(0, 0, 0);
		const FVector MyLoc = trackedTF.GetTranslation();
		levelPlatform = GetWorld()->SpawnActor<ALevel0>(levelPlatformInstance, MyLoc, MyRot);
		FVector scale = FVector(1.5f, 1.5f, levelPlatform->GetActorScale3D().Z);
		levelPlatform->SetIsPlatform();
		levelPlatform->SetObjectMobility(EComponentMobility::Movable);
		levelPlatform->SetObjectScale(scale);
		levelPlatform->SetPhysicsSimulation(false);

		HelloARManager->SetPlaneColourTransparent();
	}

	// Set the spawned actor location based on the Pin.
	levelPlatform->SetActorTransform(trackedTF);
	levelPlatform->PinComponent = UARBlueprintLibrary::PinComponent(nullptr, trackedTF, traceResult.GetValue().GetTrackedGeometry());
}

TOptional<FARTraceResult> ACustomGameMode::LineTrace(FVector screenPos)
{
	const APlayerController* playerController = GetPlayerController();
	FVector worldPos;
	FVector worldDir;

	UGameplayStatics::DeprojectScreenToWorld(playerController, FVector2D(screenPos), worldPos, worldDir);
	auto traceResult = UARBlueprintLibrary::LineTraceTrackedObjects(FVector2D(screenPos), false, false, false, true);
	
	if (traceResult.IsValidIndex(0))
	{
		return traceResult[0];
	}
	else
	{
		// GEngine->AddOnScreenDebugMessage(-1, 2.0f, FColor::Red, TEXT("Invalid line trace index"));
		return TOptional<FARTraceResult>();
	}
}

/*Setters*/
//Useful way to get reference between classes
void ACustomGameMode::SetPlayerReference(AThePlayer* pRef)
{
	playerRef = pRef;
}

void ACustomGameMode::ResetLevel()
{
	level = nullptr;
}

void ACustomGameMode::PauseARManager(bool val)
{
	//HelloARManager->PauseSession(val);
	HelloARManager->EnablePlaneUpdate(val);
}


//Getters
ACustomGameMode* ACustomGameMode::GetCustomGameModeRef()
{
	return instance;
}

APlayerController* ACustomGameMode::GetPlayerController()
{
	return UGameplayStatics::GetPlayerController(this, 0);
}

FRotator ACustomGameMode::GetDeviceRotation()
{
	FRotator rotator;
	if(UHeadMountedDisplayFunctionLibrary::IsHeadMountedDisplayEnabled())
	{
		//Get the HMD orientation (gyroscopic features)
		FRotator hmdRotation;
		FVector hmdPosition;
		UHeadMountedDisplayFunctionLibrary::GetOrientationAndPosition(hmdRotation, hmdPosition);
		rotator = hmdRotation;
	}
	
	return rotator;
}

AThePlayer* ACustomGameMode::GetPlayerReference()
{
	return playerRef;
}

AHelloARManager* ACustomGameMode::GetHelloARManager()
{
	return HelloARManager;
}








