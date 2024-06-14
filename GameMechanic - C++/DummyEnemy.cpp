// Fill out your copyright notice in the Description page of Project Settings.

#include "DummyEnemy.h"

// Sets default values
ADummyEnemy::ADummyEnemy()
{
 	// Set this character to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	// Set size for collision capsule
	GetCapsuleComponent()->InitCapsuleSize(0.f, 96.0f);

	//Initialise the enemy dummies here
	mesh_comp = CreateDefaultSubobject<USkeletalMeshComponent>(TEXT("Dummy"));
	mesh_comp->SetCollisionResponseToAllChannels(ECollisionResponse::ECR_Overlap);
	mesh_comp->SetupAttachment(RootComponent);
	static ConstructorHelpers::FObjectFinder<USkeletalMesh>mesh_s(TEXT("/Game/Characters/Mannequins/Meshes/SKM_Quinn.SKM_Quinn"));

	if (mesh_s.Succeeded())
	{
		mesh_comp->SetSkeletalMesh(mesh_s.Object);
		mesh_comp->SetNotifyRigidBodyCollision(true);
		mesh_comp->SetGenerateOverlapEvents(true);
		mesh_comp->SetEnableGravity(true);
		mesh_comp->SetCollisionProfileName(FName("Pawn"));
		mesh_comp->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
		mesh_comp->SetMassScale(FName("Mass"), 100.f);
		FRotator rotation(0.f, 90.f, 0.f);
		mesh_comp->SetRelativeLocation(FVector(0.f, 0.f, -90.f));
		mesh_comp->SetRelativeRotation(rotation);
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("Skeletal Mesh not found"));
	}

	//Initialise the hit which will be invisible in scene
	hit_box = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Hit Box"));
	hit_box->SetCollisionResponseToAllChannels(ECollisionResponse::ECR_Overlap);
	hit_box->SetupAttachment(RootComponent);
	static ConstructorHelpers::FObjectFinder<UStaticMesh>hit_mesh(TEXT("/Game/StarterContent/Shapes/Shape_Cube.Shape_Cube"));
	if (hit_mesh.Succeeded())
	{
		hit_box->SetStaticMesh(hit_mesh.Object);
		hit_box->SetNotifyRigidBodyCollision(true);
		hit_box->SetGenerateOverlapEvents(true);
		hit_box->SetEnableGravity(false);
		hit_box->SetCollisionProfileName(FName("BlockAllDynamic"));
		hit_box->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
		hit_box->SetRelativeLocation(FVector(0.f, 0.f, -90.f));
		hit_box->SetWorldScale3D(FVector(0.3f, 0.35f, 1.6f));
		hit_box->SetMassScale(FName("Mass"), 3.f);

	}

	//Initialise the particle system for the throwable object
	particle_sys = CreateDefaultSubobject<UParticleSystemComponent>(TEXT("ParticleSystem"));
	particle_sys->SetupAttachment(mesh_comp);

	//Assign to component
	static ConstructorHelpers::FObjectFinder<UParticleSystem>particle_asset(TEXT("/Game/FXVarietyPack/Particles/P_ky_waterBallHit.P_ky_waterBallHit"));
	if (particle_asset.Succeeded())
	{
		particle_sys->SetTemplate(particle_asset.Object);
		particle_sys->SetRelativeScale3D(FVector(3));
		particle_sys->bAutoActivate = false;
	}

	hit_box->OnComponentHit.AddDynamic(this, &ADummyEnemy::OnHit);
	hit_box->SetVisibility(false);

}

// Called when the game starts or when spawned
void ADummyEnemy::BeginPlay()
{
	Super::BeginPlay();
	hit_box->SetHiddenInGame(true);
}

// Called every frame
void ADummyEnemy::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

// Called to bind functionality to input
void ADummyEnemy::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

}

//Collision code for destroying the throwable
void ADummyEnemy::OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp,
	FVector NormalImpulse, const FHitResult& Hit)
{
	//Delay before destroying object so user can see the explosion
	if (health > 0)
	{
		health -= damage;
		FString health_str = FString::SanitizeFloat(health);
		GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow, health_str);
	}
	else
	{
		FTimerHandle t;
		float duration = 0.7f;
		particle_sys->Activate();
		mesh_comp->DestroyComponent();
		GetWorld()->GetTimerManager().SetTimer(t, this, &ADummyEnemy::DestroyActor, duration, false);

		GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow,
			TEXT("Calling destroy"));
	}
}

void ADummyEnemy::DestroyActor()
{
	GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Yellow,
		TEXT("Dead"));
	Destroy();
}

