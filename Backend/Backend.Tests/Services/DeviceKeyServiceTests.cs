using Backend.Services;

namespace Backend.Tests.Services
{
    public class DeviceKeyServiceTests
    {
        private readonly DeviceKeyService _deviceKeyService = new();

        [Fact]
        public void NormalizeMacAddress_WithColons_RemovesColons()
        {
            Assert.Equal("AABBCCDDEEFF", _deviceKeyService.NormalizeMacAddress("AA:BB:CC:DD:EE:FF"));
        }

        [Fact]
        public void NormalizeMacAddress_WithDashes_RemovesDashes()
        {
            Assert.Equal("AABBCCDDEEFF", _deviceKeyService.NormalizeMacAddress("AA-BB-CC-DD-EE-FF"));
        }

        [Fact]
        public void NormalizeMacAddress_Lowercase_ReturnsUppercase()
        {
            Assert.Equal("AABBCCDDEEFF", _deviceKeyService.NormalizeMacAddress("aabbccddeeff"));
        }

        [Fact]
        public void NormalizeMacAddress_SurroundingSpaces_TrimsSpaces()
        {
            Assert.Equal("AABBCCDDEEFF", _deviceKeyService.NormalizeMacAddress("  AABBCCDDEEFF "));
        }

        [Fact]
        public void Derive_SameInput_ReturnsSameKey()
        {
            string first = _deviceKeyService.Derive("master", "AABBCCDDEEFF");
            string second = _deviceKeyService.Derive("master", "AABBCCDDEEFF");

            Assert.Equal(first, second);
        }

        [Fact]
        public void Derive_DifferentFormatsOfSameMacAddress_ReturnsSameKey()
        {
            string plain = _deviceKeyService.Derive("master", "AABBCCDDEEFF");
            string formatted = _deviceKeyService.Derive("master", "aa:bb:cc:dd:ee:ff");

            Assert.Equal(plain, formatted);
        }

        [Fact]
        public void Derive_DifferentMacAddress_ReturnsDifferentKey()
        {
            string first = _deviceKeyService.Derive("master", "AABBCCDDEE01");
            string second = _deviceKeyService.Derive("master", "AABBCCDDEE02");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Derive_DifferentMasterKey_ReturnsDifferentKey()
        {
            string first = _deviceKeyService.Derive("master-one", "AABBCCDDEEFF");
            string second = _deviceKeyService.Derive("master-two", "AABBCCDDEEFF");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Derive_AnyInput_ReturnsSha256AsHex()
        {
            string key = _deviceKeyService.Derive("master", "AABBCCDDEEFF");

            Assert.Matches("^[0-9A-F]{64}$", key);
        }
    }
}
