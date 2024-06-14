// Fill out your copyright notice in the Description page of Project Settings.


#include "AThrowable.h"

// Sets default values
AAThrowable::AAThrowable()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	//Initialise the throwable object
	mesh_comp = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Ice Ball"));
	mesh_comp->SetCollisionResponseToAllChannels(ECollisionResponse::ECR_Overlap);
	mesh_comp->SetupAttachment(RootComponent);
	static ConstructorHelpers::FObjectFinder<UStaticMesh>mesh_s(TEXT("/Game/StarterContent/Shapes/Shape_Sphere.Shape_Sphere"));
	if (mesh_s.Succeeded())
	{
		mesh_comp->SetNotifyRigidBodyCollision(true);
		mesh_comp->SetGenerateOverlapEvents(true);
		mesh_comp->SetEnableGravity(false);
		mesh_comp->SetCollisionProfileName(FName("BlockAllDynamic"));
		mesh_comp->SetCollisionEnabled(ECollisionEnabled::QueryAndPhysics);
		mesh_comp->SetStaticMesh(mesh_s.Object);
		mesh_comp->SetRelativeLocation(FVector(0.f, 0.f, 0.f));
		mesh_comp->SetWorldScale3D(FVector(0.5f));
		mesh_comp->SetMassScale(FName("Mass"), 3.f);

	}

	//Initialise the particle system for the throwable object
	particle_sys = CreateDefaultSubobject<UParticleSystemComponent>(TEXT("ParticleSystem"));
	particle_sys->SetupAttachment(mesh_comp);

	//Assign to component
	static ConstructorHelpers::FObjectFinder<UParticleSystem>particle_asset(TEXT("/Game/FXVarietyPack/Particles/P_ky_waterBall.P_ky_waterBall"));
	if (particle_asset.Succeeded())
	{
		particle_sys->SetTemplate(particle_asset.Object);
		particle_sys->SetRelativeScale3D(FVector(3));
	}


	//Emitter for the object destruction animation
	explosion_emitter = CreateDefaultSubobject<UParticleSystemComponent>(TEXT("ParticleSysDestroy"));
	explosion_emitter->SetupAttachment(mesh_comp);

	static ConstructorHelpers::FObjectFinder<UParticleSystem>destruction_asset(TEXT("/Game/FXVarietyPack/Particles/P_ky_waterBallHit.P_ky_waterBallHit"));

	if (destruction_asset.Succeeded())
	{
		explosion_emitter->SetTemplate(destruction_asset.Object);
		explosion_emitter->SetRelativeScale3D(FVector(1.5));
		explosion_emitter->bAutoActivate = false;
	}
	
	mesh_comp->OnComponentHit.AddDynamic(this, &AAThrowable::OnHit);
	mesh_comp->SetHiddenInGame(true);
	mesh_comp->SetVisibility(false);
}

// Called when the game starts or when spawned
void AAThrowable::BeginPlay()
{
	Super::BeginPlay();
	//Set these parameters after the initialisation period otherwise they will not take effect
	mesh_comp->SetSimulatePhysics(true);
}

// Called every frame
void AAThrowable::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

//Collision code for destroying the throwable
void AAThrowable::OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp, 
	FVector NormalImpulse, const FHitResult& Hit)
{
	//Delay before destroying object so user can see the explosion
	FTimerHandle t;
	float duration = 0.5f;
	particle_sys->DestroyComponent();
	explosion_emitter->Activate();
	mesh_comp->DestroyComponent();
	GetWorld()->GetTimerManager().SetTimer(t, this, &AAThrowable::DestroyActor, duration, false);
}

void AAThrowable::DestroyActor()
{
	Destroy();
}

