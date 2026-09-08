public static class TestCharacterFactory
{
    public static CharacterData Create(
        string id,
        int maxHP = 30,
        int minSpeed = 5,
        int maxSpeed = 5,
        string displayName = null
    )
    {
        return new CharacterData(
            string.IsNullOrEmpty(displayName)
                ? id
                : displayName,
            maxHP,
            minSpeed,
            maxSpeed,
            id
        );
    }
}
