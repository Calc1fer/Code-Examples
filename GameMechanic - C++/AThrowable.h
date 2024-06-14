// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "Engine/StaticMesh.h"
#include "Components/StaticMeshComponent.h"
#include "Particles/ParticleSystemComponent.h"
#include "Particles/ParticleSystem.h"
#include "Kismet/GameplayStatics.h"
#include "AThrowable.generated.h"

UCLASS()
class GAMEMECHANICFINAL_API AAThrowable : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	AAThrowable();
	UFUNCTION()
	void OnHit(UPrimitiveComponent* HitComponent, AActor* OtherActor, UPrimitiveComponent* OtherComp,
		FVector NormalImpulse, const FHitResult& Hit);
	void DestroyActor();
	UStaticMeshComponent* GetMeshComp() { return mesh_comp; }
	
protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	//Variables go here
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UStaticMeshComponent* mesh_comp;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UParticleSystemComponent* particle_sys;
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Assets")
		UParticleSystemComponent* explosion_emitter;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

};
