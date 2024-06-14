// Fill out your copyright notice in the Description page of Project Settings.


#include "TicTac.h"

#include "Level0.h"
#include "Kismet/GameplayStatics.h"

// Sets default values
ATicTac::ATicTac()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	SetActorEnableCollision(true);
	
	sceneComponent = CreateDefaultSubobject<USceneComponent>(TEXT("SceneComponent"));
	SetRootComponent(sceneComponent);
	sceneComponent->SetMobility(EComponentMobility::Movable);
	
	staticMeshComponent = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("StaticMeshComponent"));
	staticMeshComponent->SetupAttachment(sceneComponent);
	
	//Settings
	staticMeshComponent->SetCollisionProfileName("BlockAllDynamic");
	staticMeshComponent->SetGenerateOverlapEvents(true);
	staticMeshComponent->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
	staticMeshComponent->SetNotifyRigidBodyCollision(true); //Stillllll a very important wee line
	staticMeshComponent->SetSimulatePhysics(false);
	staticMeshComponent->SetEnableGravity(false);
	staticMeshComponent->SetWorldScale3D(scale);

	//Lock the rotation on the x and y
	staticMeshComponent->BodyInstance.bLockYRotation = true;
	staticMeshComponent->BodyInstance.bLockXRotation = true;
	
	auto MeshAsset = ConstructorHelpers::FObjectFinder<UStaticMesh>(TEXT("StaticMesh'/Engine/BasicShapes/Sphere.Sphere'"));
	staticMeshComponent->SetStaticMesh(MeshAsset.Object);
	
	//Set the material
	ConstructorHelpers::FObjectFinder<UMaterial>FoundMaterial(TEXT("Material'/Game/Assets/Materials/Andy_Mat_Default.Andy_Mat_Default'"));
	
	if(FoundMaterial.Succeeded())
	{
		ticTacMaterial = FoundMaterial.Object;
	}
	
	ticTacMatInstance = UMaterialInstanceDynamic::Create(ticTacMaterial, staticMeshComponent);
	staticMeshComponent->SetMaterial(0, ticTacMatInstance);

	customGameMode = Cast<ACustomGameMode>(UGameplayStatics::GetGameMode(this));
}

// Called when the game starts or when spawned
void ATicTac::BeginPlay()
{
	Super::BeginPlay();

	//Get reference of the player
	playerRef = customGameMode->GetPlayerReference();
}

// Called every frame
void ATicTac::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
	
	FVector tictacLoc = staticMeshComponent->GetComponentLocation();

	// GEngine->AddOnScreenDebugMessage(-1, 5.f, FColor::Yellow, FString::Printf(TEXT("Tictac Location: %s"), *tictacLoc.ToString()));

	//Make sure the tic tacs always face the player
	if(playerRef)
	{
		//Get the location of the player
		FVector playerLoc = GetWorld()->GetFirstPlayerController()->PlayerCameraManager->GetCameraLocation();

		//Calculate the direction vector from the tic tac to the player
		FVector directionToPlayer = (playerLoc - tictacLoc).GetSafeNormal();

		//Calculate the rotation from the direction vector
		FRotator newRot = directionToPlayer.Rotation();

		//Only rotate on the z axis
		newRot.Pitch = 0.f;
		newRot.Roll = 0.f;

		//Set the rotation of the tic tac
		staticMeshComponent->SetWorldRotation(newRot);

		// DrawDebugLine(
		// GetWorld(),
		// tictacLoc,
		// playerLoc,
		// FColor::Green, // Line color
		// false, // Persistent (false means the line will disappear after one frame)
		// 0, // Depth priority
		// 0, // Thickness
		// 0.f // Time to persist (in seconds, use 0 to persist indefinitely)
		// );

		//Shoot the projectile at the player after reaching the delay
		elapsedTime += GetWorld()->DeltaTimeSeconds;
		if(elapsedTime > delay)
		{
			elapsedTime = 0.f;
			FireProjectile(directionToPlayer);

			delay = FMath::RandRange(1, 4);
		}
	}
}

void ATicTac::FireProjectile(FVector direction)
{
	if(!HasLineOfSight()) return;
	//Fire a projectile at the player after a specific time
	FVector loc = staticMeshComponent->GetComponentLocation();
	FRotator rot = FRotator::ZeroRotator;
	AProjectile* projectile = GetWorld()->SpawnActor<AProjectile>(AProjectile::StaticClass(), loc, rot);

	//Send the projectile in the direction of the player
	FVector projectileScale = FVector(0.05f,0.05f,0.05f);
	projectile->GetStaticMeshComponent()->SetWorldScale3D(projectileScale);
	projectile->SetPhysicsSimulation(true);
	projectile->GetStaticMeshComponent()->SetEnableGravity(false);

	float force = 300.f;
	projectile->GetStaticMeshComponent()->SetPhysicsLinearVelocity(direction * force);
	staticMeshComponent->SetPhysicsLinearVelocity(FVector(0.f, 0.f, staticMeshComponent->GetComponentVelocity().Z));
}

bool ATicTac::HasLineOfSight()
{
	if (!playerRef)
		return false;

	FVector StartLocation = staticMeshComponent->GetComponentLocation();
	FVector EndLocation = playerRef->staticMeshComponent->GetComponentLocation();

	FHitResult HitResult;

	// Specify collision parameters
	FCollisionQueryParams CollisionParams;
	CollisionParams.AddIgnoredActor(this); // Ignore self

	// Perform the line trace
	bool bHit = GetWorld()->LineTraceSingleByChannel(
		HitResult,
		StartLocation,
		EndLocation,
		ECC_Visibility,
		CollisionParams
	);

	if (bHit)
	{
		// Check if the hit actor is the player
		AActor* HitActor = HitResult.GetActor();
		if (HitActor && HitActor == playerRef)
		{
			return true; // Player is in line of sight
		}
	}
	return false; // No line of sight to the player
}

void ATicTac::SetPhysicsSimulation(bool val)
{
	staticMeshComponent->SetSimulatePhysics(val);
	staticMeshComponent->SetEnableGravity(val);
}

//Collision detection functions
void ATicTac::NotifyHit(UPrimitiveComponent* comp, AActor* other, UPrimitiveComponent* otherComp, bool bSelfMoved,
FVector hitLocation, FVector hitNormal, FVector normalImpulse, const FHitResult& hit)
{
	//Deal with the collisions
	Super::NotifyHit(comp, other, otherComp, bSelfMoved, hitLocation, hitNormal, normalImpulse, hit);

	//Check for if the actor is in the level class
	if(other->IsA(ALevel0::StaticClass()))
	{
		ALevel0* level = Cast<ALevel0>(other);
		if(level)
		{
			UStaticMeshComponent* otherMeshComp = Cast<UStaticMeshComponent>(hit.Component.Get());

			//If the tic tac is touching the platform then destroy it
			if(!level->GetIsPlatform()) return;
			//Call the collision response function
			Destroy();
		}
	}
}