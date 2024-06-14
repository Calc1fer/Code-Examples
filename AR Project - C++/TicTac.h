// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "ThePlayer.h"
#include "GameFramework/Actor.h"
#include "Projectile.h"
#include "TicTac.generated.h"

UCLASS()
class UE5_AR_API ATicTac : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	ATicTac();
	void SetPhysicsSimulation(bool val);
	void FireProjectile(FVector direction);

	UFUNCTION()
	virtual void NotifyHit(class UPrimitiveComponent* comp, AActor* other, UPrimitiveComponent* otherComp, bool bSelfMoved,
	FVector hitLocation, FVector hitNormal, FVector normalImpulse, const FHitResult& hit) override;
	
protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;
	bool HasLineOfSight();
	
	UStaticMeshComponent* staticMeshComponent;
public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

private:
	USceneComponent* sceneComponent;
	UMaterial* ticTacMaterial;
	UMaterialInstanceDynamic* ticTacMatInstance;
	FVector scale = FVector(0.2f,0.2,0.4f);

	AThePlayer* playerRef;
	ACustomGameMode* customGameMode;

	float elapsedTime = 0.f;
	float delay = FMath::RandRange(1, 4);
};
