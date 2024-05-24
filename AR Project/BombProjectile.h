// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "VoiceModule.h"
#include "Projectile.h"
#include "BombProjectile.generated.h"

/**
 * 
 */
UCLASS()
class UE5_AR_API ABombProjectile : public AProjectile
{
	GENERATED_BODY()
	
public:
	ABombProjectile();
	
	virtual void Tick(float DeltaSeconds) override;
	
	virtual void NotifyHit(class UPrimitiveComponent* comp, AActor* other, UPrimitiveComponent* otherComp, bool bSelfMoved,
	FVector hitLocation, FVector hitNormal, FVector normalImpulse, const FHitResult& hit) override;
	
protected:
	virtual void BeginPlay() override;
	virtual void PostInitializeComponents() override;
	void VoiceCaptureTick();
	float DetermineAmplitude(const TArray<uint8>& audioData);
	float DetermineFrequency(const TArray<uint8>& audioData);

private:
	void SparkBomb();
	void ApplyExplosiveForce(const FVector& ExplosionLocation);
	
	TSharedPtr<IVoiceCapture> voiceCapture;
	TArray<uint8> voiceCaptureBuffer;
	float voiceCaptureVolume;
	UAudioComponent* micComponent;
	UMaterial* projectileMaterialLit;
	UMaterial* projectileMaterialUnlit;
	UMaterialInstanceDynamic* projectileMatInstance;
	
	//Const threshold value for the detecting of blowing sounds
	const float blowingThreshold = 90.f;
	const float frequencyThreshold = 8000.f; // Hz
	bool sparking = false;

	float captureInterval = 0.5f;
	float elapsedTime = 0.f;
	int damage = 100;
	int scoreIncrement = 200;
};
