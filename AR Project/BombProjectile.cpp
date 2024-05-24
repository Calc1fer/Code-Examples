// Fill out your copyright notice in the Description page of Project Settings.


#include "BombProjectile.h"

#include "cmath"
#include "Level0.h"
#include "Components/AudioComponent.h"
#include "Kismet/GameplayStatics.h"

ABombProjectile::ABombProjectile()
{
	//Set the new material here and particle effects

	//Create a procedural sound wave
	micComponent = CreateDefaultSubobject<UAudioComponent>(TEXT("MicrophoneComponent"));
	micComponent->bAutoActivate = true;

	auto MeshAsset = ConstructorHelpers::FObjectFinder<UStaticMesh>(TEXT("StaticMesh'/Engine/BasicShapes/Sphere.Sphere'"));
	staticMeshComponent->SetStaticMesh(MeshAsset.Object);

	//Set the material
	ConstructorHelpers::FObjectFinder<UMaterial>FoundMaterial(TEXT("Material'/Game/Assets/Materials/BombProjectileMat.BombProjectileMat'"));

	if(FoundMaterial.Succeeded())
	{
		projectileMaterialLit = FoundMaterial.Object;
	}
	
	ConstructorHelpers::FObjectFinder<UMaterial>FoundMaterial2(TEXT("Material'/Game/Assets/Materials/BombProjectileUnlit.BombProjectileUnlit'"));
	
	if(FoundMaterial2.Succeeded())
	{
		projectileMaterialUnlit = FoundMaterial2.Object;
	}
	
	projectileMatInstance = UMaterialInstanceDynamic::Create(projectileMaterialUnlit, staticMeshComponent);
	staticMeshComponent->SetMaterial(0, projectileMatInstance);
}

void ABombProjectile::PostInitializeComponents()
{
	Super::PostInitializeComponents();
	
	//Create the UAudioComponent
	micComponent = FindComponentByClass<UAudioComponent>();
	if (!micComponent)
	{
		micComponent = NewObject<UAudioComponent>(this);
		micComponent->RegisterComponent();
	}
}


void ABombProjectile::BeginPlay()
{
	Super::BeginPlay();

	voiceCapture = FVoiceModule::Get().CreateVoiceCapture("",44100, 1);
	if(voiceCapture.IsValid())
	{
		bool success = voiceCapture->Start();
	}
}

void ABombProjectile::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);
	
	VoiceCaptureTick();
}

void ABombProjectile::VoiceCaptureTick()
{
	if(!voiceCapture.IsValid())
	{
		return;
	}

	uint32 voiceCaptureBytesAvailable;
	EVoiceCaptureState::Type captureState = voiceCapture->GetCaptureState(voiceCaptureBytesAvailable);

	voiceCaptureBuffer.Reset();
	if(captureState == EVoiceCaptureState::Ok && voiceCaptureBytesAvailable > 0)
	{
		int16 voiceCaptureSample = 0;
		uint32 voiceCaptureReadBytes = 0;
		float voiceCaptureTotalSquared = 0.f;
		
		voiceCaptureBuffer.SetNumUninitialized(voiceCaptureBytesAvailable);
		
		voiceCapture->GetVoiceData(voiceCaptureBuffer.GetData(), voiceCaptureBytesAvailable, voiceCaptureReadBytes);

		for (uint32 i = 0; i + 1 < voiceCaptureReadBytes && (i + 1) < static_cast<uint32>(voiceCaptureBuffer.Num()); i += 2)
		{
			uint16 MSB = voiceCaptureBuffer[i + 1];
			uint16 LSB = voiceCaptureBuffer[i];
    
			voiceCaptureSample = (MSB << 8) | LSB;
			voiceCaptureTotalSquared += ((float)voiceCaptureSample * (float)voiceCaptureSample);
		}
		
		float voiceCaptureMeanSqr = (2 * (voiceCaptureTotalSquared / voiceCaptureBuffer.Num()));
		float voiceCaptureRms = FMath::Sqrt(voiceCaptureMeanSqr);
		float voiceCaptureFinalVolume = ((voiceCaptureRms / 32768.0) * 200.f);
		
		voiceCaptureVolume = voiceCaptureFinalVolume;

		//Get the average amplitude and frequency and use this to spark or extinguish the bomb projectile
		float averageAmplitude = DetermineAmplitude(voiceCaptureBuffer);
		float frequencyPeak = DetermineFrequency(voiceCaptureBuffer);

		elapsedTime += GetWorld()->DeltaTimeSeconds;
		if (elapsedTime >= captureInterval)
		{
			// Reset the elapsed time for the next second
			elapsedTime = 0.0f;

			//Check if the average amplitude exceeds the blowing threshold
			if(averageAmplitude < blowingThreshold && (frequencyPeak > frequencyThreshold))
			{
				//Blowing action registered, call the spark bomb function
				SparkBomb();
			}
		}
	}
}

void ABombProjectile::NotifyHit(UPrimitiveComponent* comp, AActor* other, UPrimitiveComponent* otherComp, bool bSelfMoved, FVector hitLocation, FVector hitNormal, FVector normalImpulse, const FHitResult& hit)
{
	// Check if the hit actor is a level block (primitive cube mesh)
	if (other && other->IsA(ALevel0::StaticClass()))
	{
		ALevel0* LevelBlock = Cast<ALevel0>(other);
		if (LevelBlock)
		{
			UStaticMeshComponent* OtherMeshComp = Cast<UStaticMeshComponent>(hit.Component.Get());

			if (LevelBlock->GetIsPlatform()) return;

			// Call the collision response function
			LevelBlock->DecrementHealth(OtherMeshComp, damage);
		}
	}

	if(playerProjectile)
	{
		//Differentiate between the player and enemy throwing projectiles
		if(playerProjectile)
		{
			AThePlayer* playerRef = customGameMode->GetPlayerReference();
			playerRef->IncrementScore(scoreIncrement);
			playerProjectile = false;
		}
	}

	// Apply explosive force
	ApplyExplosiveForce(hitLocation);

	// Destroy the projectile
	Destroy();
}

void ABombProjectile::ApplyExplosiveForce(const FVector& ExplosionLocation)
{
	// Define parameters for the sweep
	float ExplosionRadius = 20.f; // Adjust the radius as needed

	// Setup the collision parameters
	FCollisionQueryParams Params;
	Params.AddIgnoredActor(this); // Ignore the bomb projectile itself
	Params.bTraceComplex = false;

	// Perform the radial sweep to find level blocks
	TArray<FHitResult> HitResults;
	bool bHit = GetWorld()->SweepMultiByChannel(
		HitResults,
		ExplosionLocation,
		ExplosionLocation,
		FQuat::Identity,
		ECC_Visibility, // Use the appropriate collision channel
		FCollisionShape::MakeSphere(ExplosionRadius),
		Params
	);

	// Iterate over the hit results
	for (const FHitResult& SweepResult : HitResults)
	{
		ALevel0* LevelBlock = Cast<ALevel0>(SweepResult.GetActor());
		if (LevelBlock)
		{
			TArray<UStaticMeshComponent*> BlockMeshComponents;
			LevelBlock->GetComponents<UStaticMeshComponent>(BlockMeshComponents);

			// Iterate through all static mesh components in the level block
			for (UStaticMeshComponent* BlockMeshComponent : BlockMeshComponents)
			{
				// Calculate the direction from the level block to the bomb projectile
				FVector ExplosionDirection = BlockMeshComponent->GetComponentLocation() - ExplosionLocation;
				ExplosionDirection.Normalize();

				// Apply an impulse to the static mesh component
				float ImpulseStrength = 20.f; // Adjust the impulse strength as needed
				BlockMeshComponent->AddImpulse(ExplosionDirection * ImpulseStrength, NAME_None, true);
			}
		}
	}
}

//The amplitude is backwards, will fix that
float ABombProjectile::DetermineAmplitude(const TArray<uint8>& audioData)
{
	//Calculate the average amplitude of the audio data
	float averageAmplitude = 0.f;
	for(uint8 sample : audioData)
	{
		averageAmplitude += FMath::Abs(sample - 128); //Centre around 128 for signed bytes
	}
	averageAmplitude /= audioData.Num();
	
	return averageAmplitude;
}

//Determine the frequency
// Determine the frequency using zero-crossing rate
float ABombProjectile::DetermineFrequency(const TArray<uint8>& audioData)
{
	// Calculate zero-crossing rate
	int32 zeroCrossings = 0;

	for (int32 index = 1; index < audioData.Num(); index++)
	{
		if ((audioData[index] - 128) * (audioData[index - 1] - 128) < 0)
		{
			zeroCrossings++;
		}
	}

	// Calculate the frequency corresponding to the zero-crossing rate
	float sampleRate = 44100.f;
	float frequency = zeroCrossings * sampleRate / (2 * audioData.Num());

	return frequency;
}

void ABombProjectile::SparkBomb()
{
	//Set the bomb to sparking or extinguish
	sparking = !sparking;

	// Create dynamic material instances
	UMaterialInstanceDynamic* projectileMatInstanceLit = UMaterialInstanceDynamic::Create(projectileMaterialLit, staticMeshComponent);
	// UMaterialInstanceDynamic* projectileMatInstanceUnlit = UMaterialInstanceDynamic::Create(projectileMaterialUnlit, staticMeshComponent);
	staticMeshComponent->SetMaterial(0, projectileMatInstanceLit);
	
	//GEngine->AddOnScreenDebugMessage(-1, 2.f, FColor::Yellow, (TEXT("Sparking!!!")));
}



