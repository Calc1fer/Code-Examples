// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CustomGameMode.h"
#include "GameFramework/Actor.h"
#include "ItemDrop.h"
#include "TicTac.h"
#include "PhysicsEngine/PhysicsHandleComponent.h"
#include "WidgetBase.h"
#include "HelloARManager.h"
#include "Level0.generated.h"

class UARPin;

UCLASS()
class UE5_AR_API ALevel0 : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	ALevel0();
	void SpawnTicTacs();
	void SetPhysicsSimulation(bool val);
	void SetObjectMobility(EComponentMobility::Type mobility);
	void SetObjectScale(const FVector& scale);
	void DecrementHealth(UStaticMeshComponent* meshComp, int damage);
	
	void SetIsPlatform();
	bool GetIsPlatform();
protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	UARPin* PinComponent;

	UPROPERTY(Category = "Level Properties", EditAnywhere, BlueprintReadWrite)
	int levelID;

private:
	void ItemDrop();
	
	ACustomGameMode* customGameMode;
	
	FVector initialScale = FVector(0.015f,0.015f, 0.015f);
	USceneComponent* sceneComponent;
	UStaticMeshComponent* staticMeshParent;
	TArray<UStaticMeshComponent*> staticMeshComponents;
	TMap<UStaticMeshComponent*, int> staticMeshHealthMap; //For storing mesh and health variables
	
	//Used for spawning the tic tacs at specific locations
	TArray<UChildActorComponent*>emptyChildActors;

	TSubclassOf<UWidgetBase> levelCompleteScreenClass;
	UWidgetBase* levelCompleteScreen;
	AHelloARManager* HelloARManager;
	
	float meshHealth = 100.f;
	int dropRate = 50;
	bool isPlatform = false;
};
