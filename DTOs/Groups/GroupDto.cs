namespace MentorEdu.DTOs.Groups
{
    public class GroupDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public List<GroupMemberDto> Members { get; set; } = new();
    }

    public class GroupMemberDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
