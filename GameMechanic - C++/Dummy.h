// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Components/StaticMeshComponent.h"
#include "Particles/ParticleSystemComponent.h"
#include "Particles/ParticleSystem.h"
#include "Kismet/GameplayStatics.h"
#include "Components/SkeletalMeshComponent.h"
#include "Dummy.generated.h"

UCLASS()
class GAMEMECHANICFINAL_API ADummy : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	ADummy();
	UFUNCTION()
		void OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp,
			FVector NormalImpulse, const FHitResult& Hit);
	void DestroyActor();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	//Variables go here
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Mesh")
		USkeletalMeshComponent* mesh_comp;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UParticleSystemComponent* particle_sys;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

};
