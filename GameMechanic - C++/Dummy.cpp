// Fill out your copyright notice in the Description page of Project Settings.


#include "Dummy.h"

// Sets default values
ADummy::ADummy()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

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
		mesh_comp->SetCollisionProfileName(FName("BlockAllDynamic"));
		mesh_comp->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
		mesh_comp->SetMassScale(FName("Mass"), 100.f);
		FRotator rotation(0.f, 90.f, 0.f);
		mesh_comp->SetRelativeRotation(rotation);
	}
	else
	{
		UE_LOG(LogTemp, Warning, TEXT("Skeletal Mesh not found"));
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

	mesh_comp->OnComponentHit.AddDynamic(this, &ADummy::OnHit);
}

// Called when the game starts or when spawned
void ADummy::BeginPlay()
{
	Super::BeginPlay();
	//Set these parameters after the initialisation period otherwise they will not take effect
	mesh_comp->SetSimulatePhysics(false);
}

// Called every frame
void ADummy::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

//Collision code for destroying the throwable
void ADummy::OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp,
	FVector NormalImpulse, const FHitResult& Hit)
{
	//Delay before destroying object so user can see the explosion
	//FTimerHandle t;
	//float duration = 0.5f;
	//particle_sys->Activate();
	//GetWorld()->GetTimerManager().SetTimer(t, this, &ADummy::DestroyActor, duration, false);
}

void ADummy::DestroyActor()
{
	Destroy();
}

