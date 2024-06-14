// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "ARTraceResult.h"
#include "HelloARManager.h"
#include "Projectile.h"
#include "GameFramework/GameModeBase.h"
#include "CustomGameMode.generated.h"

//Forward Declarations
class APlaceableActor;
class AThePlayer;
class AProjectile;
class ABombProjectile;
class ALevel0;


UCLASS()
class UE5_AR_API ACustomGameMode : public AGameModeBase
{
	GENERATED_BODY()
	
public:
	ACustomGameMode();
	virtual ~ACustomGameMode() = default;
	
	ACustomGameMode* GetCustomGameModeRef();
	virtual void StartPlay() override;

	UFUNCTION(BlueprintCallable, Category = "GameModeBase")
	void SetLevelIndex(int val);

	UFUNCTION(BlueprintCallable, Category = "GameModeBase")
	void PauseARManager(bool val);
	
	UFUNCTION(BlueprintNativeEvent, Category = "GameModeBase", DisplayName = "Start Play")
		void StartPlayEvent();
	
	 UPROPERTY(Category="Placeable",EditAnywhere,BlueprintReadWrite)
	 TSubclassOf<APlaceableActor> PlaceableToSpawn;
	
	virtual void Tick(float DeltaSeconds) override;
	virtual void SpawnInitialActors();
	
	virtual void SpawnLevel(FVector screenPos);
	virtual void SpawnProjectile(FVector screenPos, ProjectileType projectileType);
	virtual void SpawnPlatform(FVector screenPos);
	
	virtual void MoveProjectile(FVector screenPos);
	void LaunchProjectile(const FVector& startPos, const FVector& endPos, float touchTime);
	
	virtual TOptional<FARTraceResult> LineTrace(FVector screenPos);
	
	void SetPlayerReference(AThePlayer* pRef);
	AThePlayer* GetPlayerReference();
	FRotator GetDeviceRotation();
	APlayerController* GetPlayerController();
	AHelloARManager* GetHelloARManager();
	void ResetLevel();

private:
	FTimerHandle Ticker;
	float projectileDistanceOffset = 100.f;

	ALevel0* level;
	ALevel0* levelPlatform;
	TArray<UClass*>levels;
	AProjectile* regularProjectile;
	AHelloARManager* HelloARManager;
	ABombProjectile* bombProjectile;
	AThePlayer* playerRef;

	static ACustomGameMode* instance; //Create a singleton instance of this class
	TSubclassOf<ALevel0> levelInstance; //Base class is the class that blueprint uses
	TSubclassOf<ALevel0> levelPlatformInstance;

	bool platformSpawned;
	int levelIndex;
};
